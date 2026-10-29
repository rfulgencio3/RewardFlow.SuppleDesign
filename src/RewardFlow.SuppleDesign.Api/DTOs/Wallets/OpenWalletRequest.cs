using System.ComponentModel.DataAnnotations;

namespace RewardFlow.SuppleDesign.Api.DTOs.Wallets;

/// <summary>HTTP input for opening a named rewards wallet.</summary>
public sealed class OpenWalletRequest
{
    /// <summary>Human-readable wallet name; surrounding whitespace is normalized by the domain.</summary>
    [Required]
    [StringLength(120, MinimumLength = 1)]
    [NotWhiteSpace]
    public string? Name { get; init; }
}
