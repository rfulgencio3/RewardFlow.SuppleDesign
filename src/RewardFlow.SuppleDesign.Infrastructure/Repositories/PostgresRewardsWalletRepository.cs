using Microsoft.EntityFrameworkCore;
using Npgsql;
using RewardFlow.SuppleDesign.Domain.Entities;
using RewardFlow.SuppleDesign.Domain.Ports;
using RewardFlow.SuppleDesign.Domain.ValueObjects;
using RewardFlow.SuppleDesign.Infrastructure.Persistence;
using RewardFlow.SuppleDesign.Infrastructure.Persistence.Models;
using RewardFlow.SuppleDesign.Domain.Services;

namespace RewardFlow.SuppleDesign.Infrastructure.Repositories;

/// <summary>Secondary adapter that persists detached rewards-wallet snapshots in PostgreSQL through EF Core.</summary>
public sealed class PostgresRewardsWalletRepository(RewardFlowDbContext context) : IRewardsWalletRepository
{
    /// <inheritdoc />
    public Task<RewardOperationKind?> FindOperationKindAsync(Guid operationId, CancellationToken cancellationToken = default) =>
        context.RewardOperations.AsNoTracking().Where(item => item.OperationId == operationId)
            .Select(item => (RewardOperationKind?)item.Kind).SingleOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<RewardTransfer?> FindTransferAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        var stored = await context.RewardTransfers.AsNoTracking()
            .SingleOrDefaultAsync(item => item.OperationId == operationId, cancellationToken);
        return stored is null ? null : new(stored.OperationId, stored.SourceWalletId, stored.DestinationWalletId,
            new TransferAmount(stored.Points), new RewardReason(stored.Reason), stored.OccurredAtUtc,
            new TransferBalances(new RewardPoints(stored.SourceBalanceAfter), new RewardPoints(stored.DestinationBalanceAfter)));
    }

    /// <inheritdoc />
    public async Task<MovementSaveResult> TrySaveTransferAsync(RewardTransfer transfer, long sourceRevision,
        long destinationRevision, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transfer);
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            await using var databaseTransaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // A unique reservation makes retries and all movement kinds share the same identity namespace.
                context.RewardOperations.Add(new() { OperationId = transfer.OperationId, Kind = RewardOperationKind.Transfer });
                await context.SaveChangesAsync(cancellationToken);
                var updates = new[]
                {
                    (Id: transfer.SourceWalletId, Revision: sourceRevision, Balance: transfer.Balances.Source.Value,
                        Before: checked(transfer.Balances.Source.Value + transfer.Amount.Points.Value), Source: true),
                    (Id: transfer.DestinationWalletId, Revision: destinationRevision, Balance: transfer.Balances.Destination.Value,
                        Before: checked(transfer.Balances.Destination.Value - transfer.Amount.Points.Value), Source: false)
                };
                foreach (var update in updates.OrderBy(item => item.Id))
                {
                    var nextRevision = checked(update.Revision + 1);
                    var affected = await context.RewardsWallets
                        .Where(item => item.Id == update.Id && item.Revision == update.Revision && item.Points == update.Before &&
                            (update.Source ? item.Status == RewardsWalletStatus.Active : item.Status != RewardsWalletStatus.Closed))
                        .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Points, update.Balance)
                            .SetProperty(item => item.Revision, nextRevision), cancellationToken);
                    if (affected != 1)
                    {
                        await databaseTransaction.RollbackAsync(cancellationToken);
                        return MovementSaveResult.RevisionConflict;
                    }
                }
                context.RewardTransfers.Add(new()
                {
                    OperationId = transfer.OperationId,
                    SourceWalletId = transfer.SourceWalletId,
                    DestinationWalletId = transfer.DestinationWalletId,
                    Points = transfer.Amount.Points.Value,
                    Reason = transfer.Reason.Value,
                    OccurredAtUtc = transfer.OccurredAtUtc,
                    SourceBalanceAfter = transfer.Balances.Source.Value,
                    DestinationBalanceAfter = transfer.Balances.Destination.Value
                });
                context.RewardTransactions.Add(ToState(new RewardTransaction(transfer.OperationId, transfer.SourceWalletId,
                    RewardTransactionType.Debit, transfer.Amount.Points, transfer.Reason, transfer.OccurredAtUtc, transfer.Balances.Source)));
                context.RewardTransactions.Add(ToState(new RewardTransaction(transfer.OperationId, transfer.DestinationWalletId,
                    RewardTransactionType.Credit, transfer.Amount.Points, transfer.Reason, transfer.OccurredAtUtc, transfer.Balances.Destination)));
                await context.SaveChangesAsync(cancellationToken);
                await databaseTransaction.CommitAsync(cancellationToken);
                return MovementSaveResult.Saved;
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                await databaseTransaction.RollbackAsync(cancellationToken);
                return MovementSaveResult.DuplicateOperation;
            }
            finally { context.ChangeTracker.Clear(); }
        });
    }
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
        if (wallet.Id != transaction.WalletId || wallet.Balance != transaction.BalanceAfter)
            throw new ArgumentException("Movement evidence must describe the wallet being saved.", nameof(transaction));
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            await using var databaseTransaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                context.RewardOperations.Add(new()
                {
                    OperationId = transaction.OperationId,
                    Kind = transaction.Type == RewardTransactionType.Credit ? RewardOperationKind.Credit : RewardOperationKind.Debit
                });
                await context.SaveChangesAsync(cancellationToken);
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
            finally { context.ChangeTracker.Clear(); }
        });
    }

    /// <inheritdoc />
    public async Task<RewardTransaction?> FindTransactionAsync(Guid operationId,
        CancellationToken cancellationToken = default)
    {
        var stored = await context.RewardTransactions.AsNoTracking()
            .Where(item => context.RewardOperations.Any(operation => operation.OperationId == item.OperationId &&
                operation.Kind != RewardOperationKind.Transfer))
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
        TransactionId = Guid.NewGuid(),
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
