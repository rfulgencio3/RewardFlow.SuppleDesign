namespace RewardFlow.SuppleDesign.Domain.ValueObjects;

/// <summary>Standalone Class: an immutable, self-validating name used to identify a wallet to people.</summary>
/// <seealso href="https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html">Eric Evans, Chapter 10: Standalone Classes</seealso>
public sealed record WalletName
{
    /// <summary>Maximum supported length in storage and public contracts.</summary>
    public const int MaximumLength = 120;

    /// <summary>Normalized non-empty wallet name.</summary>
    public string Value { get; }

    /// <summary>Assertions: trims and validates a wallet name at the domain boundary.</summary>
    public WalletName(string value)
    {
        Value = DomainAssertions.WalletName(value, MaximumLength);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
