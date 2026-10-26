using RewardFlow.SuppleDesign.Domain.Entities;

namespace RewardFlow.SuppleDesign.Api.DTOs.Wallets;

/// <summary>HTTP representation of the wallet's descriptive state.</summary>
public sealed record WalletDetailsResponse(
    Guid WalletId,
    string Name,
    RewardsWalletStatus Status,
    DateTimeOffset CreatedAtUtc,
    long Points);
