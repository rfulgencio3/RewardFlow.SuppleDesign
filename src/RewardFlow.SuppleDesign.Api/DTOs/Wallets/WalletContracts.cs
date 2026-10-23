using System.ComponentModel.DataAnnotations;
using RewardFlow.SuppleDesign.Domain.Entities;

namespace RewardFlow.SuppleDesign.Api.DTOs.Wallets;

/// <summary>Conceptual Contours: HTTP input validation is separate from domain assertions.</summary>
public sealed class PointsRequest
{
    /// <summary>Intention-Revealing Interfaces: whole points to credit or redeem; omission and zero are invalid.</summary>
    [Required]
    [Range(typeof(long), "1", "9223372036854775807")]
    public long? Points { get; init; }
}

/// <summary>HTTP input for opening a named rewards wallet.</summary>
public sealed class OpenWalletRequest
{
    /// <summary>Human-readable wallet name; surrounding whitespace is normalized by the domain.</summary>
    [Required]
    [StringLength(120, MinimumLength = 1)]
    [NotWhiteSpace]
    public string? Name { get; init; }
}

/// <summary>HTTP validation that rejects text containing only whitespace without leaking a regex into OpenAPI.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotWhiteSpaceAttribute : ValidationAttribute
{
    /// <inheritdoc />
    public override bool IsValid(object? value) =>
        value is not string text || !string.IsNullOrWhiteSpace(text);
}

/// <summary>Minimal creation response; HTTP 201 and Location carry the remaining creation semantics.</summary>
public sealed record OpenWalletResponse(Guid WalletId);

/// <summary>Conceptual Contours: public HTTP balance contract independent of domain entities.</summary>
public sealed record WalletBalanceResponse(long Points);

/// <summary>HTTP representation of the wallet's descriptive state.</summary>
public sealed record WalletDetailsResponse(
    Guid WalletId,
    string Name,
    RewardsWalletStatus Status,
    DateTimeOffset CreatedAtUtc,
    long Points);

/// <summary>HTTP response for a prospective reward redemption query.</summary>
public sealed record WalletAffordabilityResponse(
    long AvailablePoints,
    long RequiredPoints,
    bool CanAfford);
