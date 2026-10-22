using System.ComponentModel.DataAnnotations;

namespace RewardFlow.SuppleDesign.Api.DTOs.Wallets;

/// <summary>Conceptual Contours: HTTP input validation is separate from domain assertions.</summary>
public sealed class PointsRequest
{
    /// <summary>Intention-Revealing Interfaces: whole points to credit or redeem; omission and zero are invalid.</summary>
    [Required]
    [Range(typeof(long), "1", "9223372036854775807")]
    public long? Points { get; init; }
}

/// <summary>Conceptual Contours: public HTTP balance contract independent of domain entities.</summary>
public sealed record WalletBalanceResponse(Guid WalletId, long Points);

/// <summary>HTTP response for a prospective reward redemption query.</summary>
public sealed record WalletAffordabilityResponse(
    Guid WalletId,
    long Balance,
    long RequiredPoints,
    bool CanAfford);
