using RewardFlow.SuppleDesign.Domain;
using RewardFlow.SuppleDesign.Domain.Entities;
using RewardFlow.SuppleDesign.Domain.Ports;

namespace RewardFlow.SuppleDesign.Application.Wallets;

/// <summary>Conceptual Contours: shared application persistence coordination.</summary>
/// <remarks>
/// This helper exists because multiple use cases share the same not-found and optimistic-concurrency policy. It owns
/// no business rule and should be removed if that coordination stops being shared.
/// </remarks>
internal static class WalletPersistence
{
    /// <summary>Prevents ordinary movement paths from replaying a transfer's individual ledger entry.</summary>
    internal static async Task<RewardTransaction?> FindMovementAsync(IRewardsWalletRepository repository,
        Guid operationId, CancellationToken cancellationToken)
    {
        if (await repository.FindOperationKindAsync(operationId, cancellationToken) == RewardOperationKind.Transfer)
            throw new WalletConflictException("The operation ID was already used for a reward transfer.");
        return await repository.FindTransactionAsync(operationId, cancellationToken);
    }
    /// <summary>Assertions: a command or query must address an existing, identified wallet.</summary>
    internal static async Task<WalletSnapshot> LoadAsync(
        IRewardsWalletRepository repository,
        Guid walletId,
        CancellationToken cancellationToken)
    {
        DomainAssertions.WalletIdentity(walletId);
        return await repository.FindAsync(walletId, cancellationToken)
            ?? throw new WalletNotFoundException(walletId);
    }

    /// <summary>Assertions: rejects stale snapshots to prevent lost updates.</summary>
    internal static async Task<WalletBalance> SaveAsync(
        IRewardsWalletRepository repository,
        WalletSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (!await repository.TrySaveAsync(snapshot.Wallet, snapshot.Revision, cancellationToken))
            throw new WalletConflictException("The rewards wallet changed concurrently. Reload its balance and retry.");

        return new(snapshot.Wallet.Balance.Value);
    }

    /// <summary>Assertions: balance and movement are persisted atomically and duplicate operations are replayed.</summary>
    internal static async Task<RewardTransaction> SaveMovementAsync(IRewardsWalletRepository repository,
        WalletSnapshot snapshot, RewardTransaction transaction, CancellationToken cancellationToken)
    {
        var result = await repository.TrySaveMovementAsync(snapshot.Wallet, transaction, snapshot.Revision, cancellationToken);
        if (result == MovementSaveResult.Saved)
            return await FindMovementAsync(repository, transaction.OperationId, cancellationToken)
                ?? throw new WalletConflictException("The saved reward operation could not be reloaded.");
        var replay = await FindMovementAsync(repository, transaction.OperationId, cancellationToken);
        if (replay is not null)
            return replay;
        if (result == MovementSaveResult.DuplicateOperation)
            throw new WalletConflictException("The reward operation was recorded concurrently but could not be reloaded.");
        throw new WalletConflictException("The rewards wallet changed concurrently. Reload its balance and retry.");
    }

    /// <summary>Maps a domain transaction to an application result without exposing persistence models.</summary>
    internal static RewardMovement ToResult(RewardTransaction transaction) => new(transaction.OperationId,
        transaction.Type, transaction.Points.Value, transaction.Reason.Value, transaction.OccurredAtUtc,
        transaction.BalanceAfter.Value);
}
