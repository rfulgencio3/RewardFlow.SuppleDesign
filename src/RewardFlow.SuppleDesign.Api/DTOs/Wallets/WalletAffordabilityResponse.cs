namespace RewardFlow.SuppleDesign.Api.DTOs.Wallets;

/// <summary>HTTP response for a prospective reward redemption query.</summary>
public sealed record WalletAffordabilityResponse(
    long AvailablePoints,
    long RequiredPoints,
    bool CanAfford);
