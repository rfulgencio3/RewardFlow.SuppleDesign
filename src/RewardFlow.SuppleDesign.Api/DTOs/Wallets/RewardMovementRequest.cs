using System.ComponentModel.DataAnnotations;

namespace RewardFlow.SuppleDesign.Api.DTOs.Wallets;

/// <summary>Conceptual Contours: HTTP input validation is separate from domain assertions.</summary>
public sealed class RewardMovementRequest
{
    /// <summary>Client-generated identity reused unchanged when retrying the same business operation.</summary>
    [Required]
    public Guid? OperationId { get; init; }

    /// <summary>Intention-Revealing Interfaces: whole points to credit or redeem; omission and zero are invalid.</summary>
    [Required]
    [Range(typeof(long), "1", "9223372036854775807")]
    public long? Points { get; init; }

    /// <summary>Business reason for earning or redeeming the points.</summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    [NotWhiteSpace]
    public string? Reason { get; init; }
}
