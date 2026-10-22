using RewardFlow.SuppleDesign.Domain.Exceptions;

namespace RewardFlow.SuppleDesign.Domain;

/// <summary>Assertions: explicit preconditions and invariants shared by the domain.</summary>
/// <seealso href="https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html">Eric Evans, Chapter 10: Assertions</seealso>
public static class DomainAssertions
{
    /// <summary>Assertions: point balances must be non-negative.</summary>
    public static void NonNegative(long points)
    {
        if (points < 0)
            throw new DomainException("negative_points", "Reward points cannot be negative.");
    }

    /// <summary>Assertions: a credit or debit must move at least one point.</summary>
    public static void Positive(long points)
    {
        if (points <= 0)
            throw new DomainException("positive_points_required", "A credit or debit must contain at least one reward point.");
    }

    /// <summary>Assertions: zero-balance and insufficient-balance protection prevent overdrafts.</summary>
    public static void SufficientBalance(long balance, long required)
    {
        if (balance < required)
            throw new DomainException("insufficient_points", "The rewards wallet cannot afford the required points.");
    }

    /// <summary>Assertions: every wallet has a meaningful identity.</summary>
    public static void WalletIdentity(Guid id)
    {
        if (id == Guid.Empty)
            throw new DomainException("invalid_wallet_id", "A rewards wallet ID cannot be empty.");
    }
}
