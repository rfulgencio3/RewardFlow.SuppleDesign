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
    private readonly Dictionary<Guid, RewardTransfer> transfers = new();

    /// <inheritdoc />
    public Task<WalletSnapshot?> FindAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (wallets)
        {
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
    }

    /// <inheritdoc />
    public Task<bool> TryAddAsync(RewardsWallet wallet, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (wallets)
        {
            return Task.FromResult(wallets.TryAdd(wallet.Id, new(
                wallet.Name.Value,
                wallet.Status,
                wallet.CreatedAtUtc,
                wallet.Balance.Value,
                0)));
        }
    }

    /// <inheritdoc />
    public Task<bool> TrySaveAsync(RewardsWallet wallet, long expectedRevision, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (wallets)
        {
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
    }

    /// <inheritdoc />
    public Task<MovementSaveResult> TrySaveMovementAsync(RewardsWallet wallet, RewardTransaction transaction,
        long expectedRevision, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (wallets)
        {
            if (transactions.ContainsKey(transaction.OperationId) || transfers.ContainsKey(transaction.OperationId))
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
        lock (wallets)
        {
            transactions.TryGetValue(operationId, out var transaction);
            return Task.FromResult(transaction);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RewardTransaction>> ListTransactionsAsync(Guid walletId, int skip, int take,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (wallets)
        {
            IReadOnlyList<RewardTransaction> result = transactions.Values.Where(item => item.WalletId == walletId)
                .Concat(transfers.Values.Where(item => item.SourceWalletId == walletId || item.DestinationWalletId == walletId)
                    .Select(item => new RewardTransaction(item.OperationId, walletId,
                        item.SourceWalletId == walletId ? RewardTransactionType.Debit : RewardTransactionType.Credit,
                        item.Amount.Points, item.Reason, item.OccurredAtUtc,
                        item.SourceWalletId == walletId ? item.Balances.Source : item.Balances.Destination)))
                .OrderByDescending(item => item.OccurredAtUtc).ThenByDescending(item => item.OperationId)
                .Skip(skip).Take(take).ToArray();
            return Task.FromResult(result);
        }
    }

    public Task<RewardOperationKind?> FindOperationKindAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (wallets)
        {
            RewardOperationKind? kind = transfers.ContainsKey(operationId) ? RewardOperationKind.Transfer :
                transactions.TryGetValue(operationId, out var entry) ?
                (entry.Type == RewardTransactionType.Credit ? RewardOperationKind.Credit : RewardOperationKind.Debit) : null;
            return Task.FromResult(kind);
        }
    }

    public Task<RewardTransfer?> FindTransferAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (wallets) { return Task.FromResult(transfers.GetValueOrDefault(operationId)); }
    }

    public Task<MovementSaveResult> TrySaveTransferAsync(RewardTransfer transfer, long sourceRevision,
        long destinationRevision, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (wallets)
        {
            if (transfers.ContainsKey(transfer.OperationId) || transactions.ContainsKey(transfer.OperationId))
                return Task.FromResult(MovementSaveResult.DuplicateOperation);
            if (!wallets.TryGetValue(transfer.SourceWalletId, out var source) || source.Revision != sourceRevision ||
                !wallets.TryGetValue(transfer.DestinationWalletId, out var destination) || destination.Revision != destinationRevision)
                return Task.FromResult(MovementSaveResult.RevisionConflict);
            if (source.Status != RewardsWalletStatus.Active || destination.Status == RewardsWalletStatus.Closed ||
                source.Points - transfer.Amount.Points.Value != transfer.Balances.Source.Value ||
                destination.Points > long.MaxValue - transfer.Amount.Points.Value ||
                destination.Points + transfer.Amount.Points.Value != transfer.Balances.Destination.Value)
                return Task.FromResult(MovementSaveResult.RevisionConflict);
            var sourceNextRevision = checked(sourceRevision + 1);
            var destinationNextRevision = checked(destinationRevision + 1);
            wallets[transfer.SourceWalletId] = source with { Points = transfer.Balances.Source.Value, Revision = sourceNextRevision };
            wallets[transfer.DestinationWalletId] = destination with { Points = transfer.Balances.Destination.Value, Revision = destinationNextRevision };
            transfers.Add(transfer.OperationId, transfer);
            return Task.FromResult(MovementSaveResult.Saved);
        }
    }
}
