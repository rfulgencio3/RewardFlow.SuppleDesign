using System.ComponentModel.DataAnnotations;

namespace RewardFlow.SuppleDesign.Api.DTOs.Wallets;

/// <summary>HTTP input for one atomic transfer; omission is distinct from an empty identity.</summary>
public sealed class RewardTransferRequest
{
    /// <summary>Global identity retained when retrying this operation.</summary>
    [Required]
    public Guid? OperationId { get; init; }
    /// <summary>Different wallet receiving the points.</summary>
    [Required]
    public Guid? DestinationWalletId { get; init; }
    /// <summary>Whole points, limited to one hundred per transfer.</summary>
    [Required]
    [Range(typeof(long), "1", "100")]
    public long? Points { get; init; }
    /// <summary>Business explanation, normalized by the domain.</summary>
    [Required, StringLength(200, MinimumLength = 1), NotWhiteSpace]
    public string? Reason { get; init; }
}
