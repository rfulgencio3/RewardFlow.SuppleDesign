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
}
