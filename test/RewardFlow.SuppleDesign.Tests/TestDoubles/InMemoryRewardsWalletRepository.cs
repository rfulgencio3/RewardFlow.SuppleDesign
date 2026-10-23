using System.Collections.Concurrent;
using RewardFlow.SuppleDesign.Domain.Entities;
using RewardFlow.SuppleDesign.Domain.Ports;
using RewardFlow.SuppleDesign.Domain.ValueObjects;

namespace RewardFlow.SuppleDesign.Tests.TestDoubles;

/// <summary>Fast test double that preserves the repository port's detached-snapshot and concurrency semantics.</summary>
internal sealed class InMemoryRewardsWalletRepository : IRewardsWalletRepository
{
    private sealed record StoredWallet(
        string Name,
        RewardsWalletStatus Status,
        DateTimeOffset CreatedAtUtc,
        long Points,
        long Revision);
    private readonly ConcurrentDictionary<Guid, StoredWallet> wallets = new();
    private readonly ConcurrentDictionary<Guid, RewardTransaction> transactions = new();

    /// <inheritdoc />
    public Task<WalletSnapshot?> FindAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        WalletSnapshot? snapshot = wallets.TryGetValue(walletId, out var stored)
            ? new(new RewardsWallet(
                walletId,
                new WalletName(stored.Name),
                new RewardPoints(stored.Points),
                stored.Status,
                stored.CreatedAtUtc), stored.Revision)
            : null;
        return Task.FromResult(snapshot);
    }

    /// <inheritdoc />
    public Task<bool> TryAddAsync(RewardsWallet wallet, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(wallets.TryAdd(wallet.Id, new(
            wallet.Name.Value,
            wallet.Status,
            wallet.CreatedAtUtc,
            wallet.Balance.Value,
            0)));
    }

    /// <inheritdoc />
    public Task<bool> TrySaveAsync(RewardsWallet wallet, long expectedRevision, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!wallets.TryGetValue(wallet.Id, out var current) || current.Revision != expectedRevision)
            return Task.FromResult(false);
        return Task.FromResult(wallets.TryUpdate(wallet.Id,
            new(
                wallet.Name.Value,
                wallet.Status,
                wallet.CreatedAtUtc,
                wallet.Balance.Value,
                checked(expectedRevision + 1)),
            current));
    }

    /// <inheritdoc />
    public Task<MovementSaveResult> TrySaveMovementAsync(RewardsWallet wallet, RewardTransaction transaction,
        long expectedRevision, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (wallets)
        {
            if (transactions.ContainsKey(transaction.OperationId))
                return Task.FromResult(MovementSaveResult.DuplicateOperation);
            if (!wallets.TryGetValue(wallet.Id, out var current) || current.Revision != expectedRevision)
                return Task.FromResult(MovementSaveResult.RevisionConflict);
            wallets[wallet.Id] = new(wallet.Name.Value, wallet.Status, wallet.CreatedAtUtc, wallet.Balance.Value,
                checked(expectedRevision + 1));
            transactions[transaction.OperationId] = transaction;
            return Task.FromResult(MovementSaveResult.Saved);
        }
    }

    /// <inheritdoc />
    public Task<RewardTransaction?> FindTransactionAsync(Guid operationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        transactions.TryGetValue(operationId, out var transaction);
        return Task.FromResult(transaction);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RewardTransaction>> ListTransactionsAsync(Guid walletId, int skip, int take,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<RewardTransaction> result = transactions.Values.Where(item => item.WalletId == walletId)
            .OrderByDescending(item => item.OccurredAtUtc).ThenByDescending(item => item.OperationId)
            .Skip(skip).Take(take).ToArray();
        return Task.FromResult(result);
    }
}
