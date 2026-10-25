using RewardFlow.SuppleDesign.Domain.ValueObjects;

namespace RewardFlow.SuppleDesign.Domain.Ports;

/// <summary>Intention-Revealing Interfaces: wallet behavior expressed in the loyalty domain's language.</summary>
/// <remarks>
/// This is a domain role contract, not an adapter seam. It is kept because naming the small wallet vocabulary is
/// central to this teaching sample; <see cref="RewardFlow.SuppleDesign.Domain.Entities.RewardsWallet"/> remains the only implementation until the
/// domain presents a real need for another one.
/// </remarks>
/// <seealso href="https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html">Eric Evans, Chapter 10: Intention-Revealing Interfaces</seealso>
public interface IRewardsWallet
{
    /// <summary>Intention-Revealing Interfaces: the wallet's unique identity.</summary>
    Guid Id { get; }
    /// <summary>Intention-Revealing Interfaces: human-readable wallet name.</summary>
    ValueObjects.WalletName Name { get; }
    /// <summary>Intention-Revealing Interfaces: current wallet lifecycle state.</summary>
    Entities.RewardsWalletStatus Status { get; }
    /// <summary>Intention-Revealing Interfaces: UTC instant at which the wallet was opened.</summary>
    DateTimeOffset CreatedAtUtc { get; }
    /// <summary>Intention-Revealing Interfaces: currently available reward points.</summary>
    RewardPoints Balance { get; }
    /// <summary>Intention-Revealing Interfaces: increases available points for an ordinary credit.</summary>
    void Credit(RewardPoints creditedPoints);
    /// <summary>Intention-Revealing Interfaces: decreases available points for an ordinary debit.</summary>
    void Debit(RewardPoints debitedPoints);
    /// <summary>Returns a validated prospective credit balance without changing this wallet.</summary>
    RewardPoints BalanceAfterCredit(RewardPoints creditedPoints);
    /// <summary>Returns a validated prospective debit balance without changing this wallet.</summary>
    RewardPoints BalanceAfterDebit(RewardPoints debitedPoints);
    /// <summary>Side-Effect-Free Functions: checks affordability without changing any state.</summary>
    bool CanAfford(RewardPoints requiredPoints);
    /// <summary>Intention-Revealing Interfaces: temporarily blocks redemptions.</summary>
    void Suspend();
    /// <summary>Intention-Revealing Interfaces: returns a suspended wallet to active use.</summary>
    void Reactivate();
    /// <summary>Intention-Revealing Interfaces: permanently closes the wallet.</summary>
    void Close();
}
