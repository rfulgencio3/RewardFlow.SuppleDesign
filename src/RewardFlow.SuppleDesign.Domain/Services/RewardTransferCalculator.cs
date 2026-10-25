using RewardFlow.SuppleDesign.Domain.Entities;
using RewardFlow.SuppleDesign.Domain.Exceptions;
using RewardFlow.SuppleDesign.Domain.ValueObjects;

namespace RewardFlow.SuppleDesign.Domain.Services;

/// <summary>Immutable result of a transfer calculation; neither wallet has been changed.</summary>
public sealed record TransferBalances(RewardPoints Source, RewardPoints Destination);

/// <summary>Domain service: relates two aggregates without taking ownership of persistence or lifecycle rules.</summary>
public static class RewardTransferCalculator
{
    /// <summary>Side-Effect-Free Function: validates both sides before producing their new balances.</summary>
    public static TransferBalances Calculate(RewardsWallet source, RewardsWallet destination, TransferAmount amount)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(amount);
        if (source.Id == destination.Id)
            throw new DomainException("same_wallet_transfer", "A transfer requires two different rewards wallets.");
        return new(source.BalanceAfterDebit(amount.Points), destination.BalanceAfterCredit(amount.Points));
    }
}
