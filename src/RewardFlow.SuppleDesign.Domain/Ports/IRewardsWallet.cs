using RewardFlow.SuppleDesign.Domain.ValueObjects;

namespace RewardFlow.SuppleDesign.Domain.Ports;

/// <summary>Intention-Revealing Interfaces: wallet behavior expressed in the loyalty domain's language.</summary>
/// <seealso href="https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html">Eric Evans, Chapter 10: Intention-Revealing Interfaces</seealso>
public interface IRewardsWallet
{
    /// <summary>Intention-Revealing Interfaces: the wallet's unique identity.</summary>
    Guid Id { get; }
    /// <summary>Intention-Revealing Interfaces: currently available reward points.</summary>
    RewardPoints Balance { get; }
    /// <summary>Intention-Revealing Interfaces: credits earned points to this wallet.</summary>
    void Credit(RewardPoints earnedPoints);
    /// <summary>Intention-Revealing Interfaces: debits points redeemed by the member.</summary>
    void Debit(RewardPoints redeemedPoints);
    /// <summary>Side-Effect-Free Functions: checks affordability without changing any state.</summary>
    bool CanAfford(RewardPoints requiredPoints);
}
