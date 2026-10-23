using Microsoft.EntityFrameworkCore;
using Npgsql;
using RewardFlow.SuppleDesign.Domain.Entities;
using RewardFlow.SuppleDesign.Domain.Ports;
using RewardFlow.SuppleDesign.Domain.ValueObjects;
using RewardFlow.SuppleDesign.Infrastructure.Persistence;
using RewardFlow.SuppleDesign.Infrastructure.Persistence.Models;

namespace RewardFlow.SuppleDesign.Infrastructure.Repositories;

/// <summary>Secondary adapter that persists detached rewards-wallet snapshots in PostgreSQL through EF Core.</summary>
public sealed class PostgresRewardsWalletRepository(RewardFlowDbContext context) : IRewardsWalletRepository
{
    /// <inheritdoc />
    public async Task<WalletSnapshot?> FindAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var stored = await context.RewardsWallets
            .AsNoTracking()
            .SingleOrDefaultAsync(wallet => wallet.Id == walletId, cancellationToken);

        return stored is null
            ? null
            : new WalletSnapshot(
                new RewardsWallet(
                    stored.Id,
                    new WalletName(stored.Name),
                    new RewardPoints(stored.Points),
                    stored.Status,
                    stored.CreatedAtUtc),
                stored.Revision);
    }

    /// <inheritdoc />
    public async Task<bool> TryAddAsync(RewardsWallet wallet, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(wallet);
        var stored = new RewardsWalletState
        {
            Id = wallet.Id,
            Name = wallet.Name.Value,
            Status = wallet.Status,
            CreatedAtUtc = wallet.CreatedAtUtc,
            Points = wallet.Balance.Value,
            Revision = 0
        };

        context.RewardsWallets.Add(stored);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            context.Entry(stored).State = EntityState.Detached;
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> TrySaveAsync(
        RewardsWallet wallet,
        long expectedRevision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(wallet);
        var nextRevision = checked(expectedRevision + 1);
        var affectedRows = await context.RewardsWallets
            .Where(stored => stored.Id == wallet.Id && stored.Revision == expectedRevision)
            .ExecuteUpdateAsync(update => update
                .SetProperty(stored => stored.Points, wallet.Balance.Value)
                .SetProperty(stored => stored.Status, wallet.Status)
                .SetProperty(stored => stored.Revision, nextRevision), cancellationToken);

        return affectedRows == 1;
    }

    /// <inheritdoc />
    public async Task<MovementSaveResult> TrySaveMovementAsync(RewardsWallet wallet, RewardTransaction transaction,
        long expectedRevision, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(wallet);
        ArgumentNullException.ThrowIfNull(transaction);
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var databaseTransaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var nextRevision = checked(expectedRevision + 1);
                var affectedRows = await context.RewardsWallets
                    .Where(stored => stored.Id == wallet.Id && stored.Revision == expectedRevision)
                    .ExecuteUpdateAsync(update => update
                        .SetProperty(stored => stored.Points, wallet.Balance.Value)
                        .SetProperty(stored => stored.Status, wallet.Status)
                        .SetProperty(stored => stored.Revision, nextRevision), cancellationToken);
                if (affectedRows != 1)
                {
                    await databaseTransaction.RollbackAsync(cancellationToken);
                    return MovementSaveResult.RevisionConflict;
                }

                context.RewardTransactions.Add(ToState(transaction));
                await context.SaveChangesAsync(cancellationToken);
                await databaseTransaction.CommitAsync(cancellationToken);
                return MovementSaveResult.Saved;
            }
            catch (DbUpdateException exception)
                when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                await databaseTransaction.RollbackAsync(cancellationToken);
                context.ChangeTracker.Clear();
                return MovementSaveResult.DuplicateOperation;
            }
        });
    }

    /// <inheritdoc />
    public async Task<RewardTransaction?> FindTransactionAsync(Guid operationId,
        CancellationToken cancellationToken = default)
    {
        var stored = await context.RewardTransactions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.OperationId == operationId, cancellationToken);
        return stored is null ? null : ToDomain(stored);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RewardTransaction>> ListTransactionsAsync(Guid walletId, int skip, int take,
        CancellationToken cancellationToken = default) =>
        await context.RewardTransactions.AsNoTracking().Where(item => item.WalletId == walletId)
            .OrderByDescending(item => item.OccurredAtUtc).ThenByDescending(item => item.OperationId)
            .Skip(skip).Take(take).Select(item => new RewardTransaction(item.OperationId, item.WalletId, item.Type,
                new RewardPoints(item.Points), new RewardReason(item.Reason), item.OccurredAtUtc,
                new RewardPoints(item.BalanceAfter))).ToListAsync(cancellationToken);

    private static RewardTransactionState ToState(RewardTransaction transaction) => new()
    {
        OperationId = transaction.OperationId,
        WalletId = transaction.WalletId,
        Type = transaction.Type,
        Points = transaction.Points.Value,
        Reason = transaction.Reason.Value,
        OccurredAtUtc = transaction.OccurredAtUtc,
        BalanceAfter = transaction.BalanceAfter.Value
    };

    private static RewardTransaction ToDomain(RewardTransactionState stored) => new(stored.OperationId,
        stored.WalletId, stored.Type, new RewardPoints(stored.Points), new RewardReason(stored.Reason),
        stored.OccurredAtUtc, new RewardPoints(stored.BalanceAfter));
}
