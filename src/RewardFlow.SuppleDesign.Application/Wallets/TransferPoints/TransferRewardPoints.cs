using RewardFlow.SuppleDesign.Domain;
using RewardFlow.SuppleDesign.Domain.Entities;
using RewardFlow.SuppleDesign.Domain.Ports;
using RewardFlow.SuppleDesign.Domain.Services;
using RewardFlow.SuppleDesign.Domain.ValueObjects;
using RewardFlow.SuppleDesign.Domain.Exceptions;

namespace RewardFlow.SuppleDesign.Application.Wallets;

/// <summary>Inbound port for a single atomic wallet-to-wallet transfer.</summary>
public interface ITransferRewardPoints
{
    /// <summary>Transfers 1..100 points or replays the original accepted result.</summary>
    Task<RewardTransferResult> ExecuteAsync(Guid sourceWalletId, Guid destinationWalletId, Guid operationId,
        long points, string reason, CancellationToken cancellationToken = default);
}

/// <summary>Coordinates two snapshots and one commit while the domain calculates the business result.</summary>
public sealed class TransferRewardPoints(IRewardsWalletRepository repository, TimeProvider clock) : ITransferRewardPoints
{
    /// <inheritdoc />
    public async Task<RewardTransferResult> ExecuteAsync(Guid sourceWalletId, Guid destinationWalletId,
        Guid operationId, long points, string reason, CancellationToken cancellationToken = default)
    {
        DomainAssertions.OperationIdentity(operationId);
        DomainAssertions.WalletIdentity(sourceWalletId);
        DomainAssertions.WalletIdentity(destinationWalletId);
        var amount = new TransferAmount(points);
        var rewardReason = new RewardReason(reason);
        var existing = await FindReplayAsync(operationId, cancellationToken);
        if (existing is not null)
            return Match(existing, sourceWalletId, destinationWalletId, amount, rewardReason);

        var source = await WalletPersistence.LoadAsync(repository, sourceWalletId, cancellationToken);
        var destination = await WalletPersistence.LoadAsync(repository, destinationWalletId, cancellationToken);
        TransferBalances balances;
        try
        {
            balances = RewardTransferCalculator.Calculate(source.Wallet, destination.Wallet, amount);
        }
        catch (DomainException)
        {
            // A matching request may commit between the initial lookup and these snapshot reads.
            var concurrent = await FindReplayAsync(operationId, cancellationToken);
            if (concurrent is not null)
                return Match(concurrent, sourceWalletId, destinationWalletId, amount, rewardReason);
            throw;
        }
        var transfer = new RewardTransfer(operationId, sourceWalletId, destinationWalletId, amount,
            rewardReason, clock.GetUtcNow(), balances);
        var saved = await repository.TrySaveTransferAsync(transfer, source.Revision, destination.Revision, cancellationToken);
        // Re-read even after conflicts: another identical request may have committed this operation.
        var accepted = await FindReplayAsync(operationId, cancellationToken);
        if (accepted is not null)
            return Match(accepted, sourceWalletId, destinationWalletId, amount, rewardReason);
        throw new WalletConflictException(saved == MovementSaveResult.Saved
            ? "The saved reward transfer could not be reloaded."
            : "The rewards wallets changed concurrently. Reload their balances and retry.");
    }

    private async Task<RewardTransfer?> FindReplayAsync(Guid operationId, CancellationToken cancellationToken)
    {
        var kind = await repository.FindOperationKindAsync(operationId, cancellationToken);
        if (kind is not null && kind != RewardOperationKind.Transfer)
            throw new WalletConflictException("The operation ID was already used for a different reward operation.");
        return await repository.FindTransferAsync(operationId, cancellationToken);
    }

    private static RewardTransferResult Match(RewardTransfer transfer, Guid source, Guid destination,
        TransferAmount amount, RewardReason reason)
    {
        if (transfer.SourceWalletId != source || transfer.DestinationWalletId != destination ||
            transfer.Amount != amount || transfer.Reason != reason)
            throw new WalletConflictException("The operation ID was already used with different transfer data.");
        return new(transfer.OperationId, transfer.SourceWalletId, transfer.DestinationWalletId,
            transfer.Amount.Points.Value, transfer.Reason.Value, transfer.OccurredAtUtc,
            transfer.Balances.Source.Value, transfer.Balances.Destination.Value);
    }
}
