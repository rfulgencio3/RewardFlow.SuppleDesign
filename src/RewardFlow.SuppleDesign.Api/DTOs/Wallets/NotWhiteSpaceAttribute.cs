using System.ComponentModel.DataAnnotations;

namespace RewardFlow.SuppleDesign.Api.DTOs.Wallets;

/// <summary>HTTP validation that rejects text containing only whitespace without leaking a regex into OpenAPI.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotWhiteSpaceAttribute : ValidationAttribute
{
    /// <inheritdoc />
    public override bool IsValid(object? value) =>
        value is not string text || !string.IsNullOrWhiteSpace(text);
}
