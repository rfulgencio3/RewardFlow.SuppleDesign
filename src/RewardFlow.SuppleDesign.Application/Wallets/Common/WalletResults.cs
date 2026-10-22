namespace RewardFlow.SuppleDesign.Application.Wallets;

/// <summary>Intention-Revealing Interfaces: application result independent of HTTP contracts.</summary>
/// <remarks>Keeps use cases reusable without making the application layer depend on API DTOs.</remarks>
public sealed record WalletBalance(Guid WalletId, long Points);

/// <summary>Query result describing whether a wallet can fund a prospective redemption.</summary>
/// <remarks>Keeps the query result independent of its current HTTP representation.</remarks>
public sealed record WalletAffordability(Guid WalletId, long Balance, long RequiredPoints, bool CanAfford);
