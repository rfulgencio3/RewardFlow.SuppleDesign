namespace RewardFlow.SuppleDesign.Domain.ValueObjects;

/// <summary>Standalone Class: immutable explanation for why reward points moved.</summary>
public sealed record RewardReason
{
    /// <summary>Maximum length shared by the domain, API contract and persistence adapter.</summary>
    public const int MaximumLength = 200;

    /// <summary>Normalized reason text.</summary>
    public string Value { get; }

    /// <summary>Assertions: creates a meaningful, bounded movement reason.</summary>
    public RewardReason(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new Exceptions.DomainException("reward_reason_required", "A reward transaction reason is required.");
        if (normalized.Length > MaximumLength)
            throw new Exceptions.DomainException("reward_reason_too_long", $"A reward transaction reason cannot exceed {MaximumLength} characters.");
        Value = normalized;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
