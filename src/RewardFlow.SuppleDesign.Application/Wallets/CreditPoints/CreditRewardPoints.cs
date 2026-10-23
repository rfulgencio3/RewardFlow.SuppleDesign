using RewardFlow.SuppleDesign.Domain.Ports;
using RewardFlow.SuppleDesign.Domain.ValueObjects;
using RewardFlow.SuppleDesign.Domain.Entities;
using RewardFlow.SuppleDesign.Domain;

namespace RewardFlow.SuppleDesign.Application.Wallets;

/// <summary>Intention-Revealing Interfaces: primary port for earned-point credits.</summary>
/// <remarks>Lets primary adapters depend on the credit capability rather than its orchestration details.</remarks>
public interface ICreditRewardPoints
{
    /// <summary>Intention-Revealing Interfaces: credits points and returns the saved balance.</summary>
    Task<RewardMovement> ExecuteAsync(Guid walletId, Guid operationId, long points, string reason,
        CancellationToken cancellationToken = default);
}

/// <summary>Conceptual Contours: coordinates a credit through the aggregate and secondary port.</summary>
public sealed class CreditRewardPoints(IRewardsWalletRepository repository, TimeProvider clock) : ICreditRewardPoints
{
    /// <inheritdoc />
    public async Task<RewardMovement> ExecuteAsync(Guid walletId, Guid operationId, long points, string reason,
        CancellationToken cancellationToken = default)
    {
        DomainAssertions.OperationIdentity(operationId);
        var earnedPoints = new RewardPoints(points);
        var rewardReason = new RewardReason(reason);
        var existing = await repository.FindTransactionAsync(operationId, cancellationToken);
        if (existing is not null)
            return EnsureSameOperation(existing, walletId, RewardTransactionType.Credit, earnedPoints, rewardReason);
        var snapshot = await WalletPersistence.LoadAsync(repository, walletId, cancellationToken);
        snapshot.Wallet.Credit(earnedPoints);
        var transaction = new RewardTransaction(operationId, walletId, RewardTransactionType.Credit, earnedPoints,
            rewardReason, clock.GetUtcNow(), snapshot.Wallet.Balance);
        var saved = await WalletPersistence.SaveMovementAsync(repository, snapshot, transaction, cancellationToken);
        return EnsureSameOperation(saved, walletId, RewardTransactionType.Credit, earnedPoints, rewardReason);
    }

    private static RewardMovement EnsureSameOperation(RewardTransaction transaction, Guid walletId,
        RewardTransactionType type, RewardPoints points, RewardReason reason)
    {
        if (transaction.WalletId != walletId || transaction.Type != type || transaction.Points != points || transaction.Reason != reason)
            throw new WalletConflictException("The operation ID was already used with different reward transaction data.");
        return WalletPersistence.ToResult(transaction);
    }
}
