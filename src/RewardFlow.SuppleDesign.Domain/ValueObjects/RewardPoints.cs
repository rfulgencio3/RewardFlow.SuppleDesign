namespace RewardFlow.SuppleDesign.Domain.ValueObjects;

/// <summary>
/// Standalone Class: immutable point arithmetic using only the BCL, with no framework or service dependencies.
/// Closure of Operations: addition and subtraction accept and return RewardPoints.
/// Conceptual Contours: all point arithmetic belongs here, separate from wallet state.
/// </summary>
/// <seealso href="https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html">Eric Evans, Chapter 10: Supple Design</seealso>
/// <seealso href="https://www.domainlanguage.com/ddd/reference/">Eric Evans' DDD Reference</seealso>
public sealed record RewardPoints
{
    /// <summary>Intention-Revealing Interfaces: the number of whole reward points.</summary>
    public long Value { get; }

    /// <summary>Side-Effect-Free Functions: a reusable immutable zero balance.</summary>
    public static RewardPoints Zero { get; } = new(0);

    /// <summary>Assertions: constructs points only when the non-negative invariant holds.</summary>
    public RewardPoints(long value)
    {
        DomainAssertions.NonNegative(value);
        Value = value;
    }

    /// <summary>Closure of Operations and Side-Effect-Free Functions: returns a new sum without changing either operand.</summary>
    public RewardPoints Add(RewardPoints other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other.Value > long.MaxValue - Value)
            throw new Exceptions.DomainException("points_limit_exceeded", "The reward points limit would be exceeded.");
        return new RewardPoints(Value + other.Value);
    }

    /// <summary>Closure of Operations and Assertions: returns a new difference and prevents negative points.</summary>
    public RewardPoints Subtract(RewardPoints other)
    {
        ArgumentNullException.ThrowIfNull(other);
        DomainAssertions.SufficientBalance(Value, other.Value);
        return new RewardPoints(Value - other.Value);
    }
}
