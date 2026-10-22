using RewardFlow.SuppleDesign.Domain.Ports;
using RewardFlow.SuppleDesign.Domain.ValueObjects;

namespace RewardFlow.SuppleDesign.Domain.Entities;

/// <summary>Conceptual Contours: aggregate state and credit/debit rules live here; point arithmetic lives in RewardPoints.</summary>
/// <seealso href="https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html">Eric Evans, Chapter 10: Conceptual Contours</seealso>
/// <seealso href="https://www.domainlanguage.com/ddd/reference/">Eric Evans' DDD Reference: Entities and Aggregates</seealso>
public sealed class RewardsWallet : IRewardsWallet
{
    /// <inheritdoc />
    public Guid Id { get; }
    /// <inheritdoc />
    public RewardPoints Balance { get; private set; }

    /// <summary>Assertions: opens an identified wallet with a zero balance.</summary>
    public RewardsWallet(Guid id) : this(id, RewardPoints.Zero) { }

    /// <summary>Assertions: reconstitutes a wallet from a valid immutable balance.</summary>
    public RewardsWallet(Guid id, RewardPoints balance)
    {
        DomainAssertions.WalletIdentity(id);
        ArgumentNullException.ThrowIfNull(balance);
        Id = id;
        Balance = balance;
    }

    /// <inheritdoc />
    public void Credit(RewardPoints earnedPoints)
    {
        ArgumentNullException.ThrowIfNull(earnedPoints);
        DomainAssertions.Positive(earnedPoints.Value);
        Balance = Balance.Add(earnedPoints);
    }

    /// <inheritdoc />
    public void Debit(RewardPoints redeemedPoints)
    {
        ArgumentNullException.ThrowIfNull(redeemedPoints);
        DomainAssertions.Positive(redeemedPoints.Value);
        Balance = Balance.Subtract(redeemedPoints);
    }

    /// <inheritdoc />
    public bool CanAfford(RewardPoints requiredPoints)
    {
        ArgumentNullException.ThrowIfNull(requiredPoints);
        return Balance.Value >= requiredPoints.Value;
    }
}
