namespace RewardFlow.SuppleDesign.Application.Wallets;

/// <summary>Intention-Revealing Interfaces: application result independent of HTTP contracts.</summary>
public sealed record WalletBalance(Guid WalletId, long Points);

/// <summary>Query result describing whether a wallet can fund a prospective redemption.</summary>
public sealed record WalletAffordability(Guid WalletId, long Balance, long RequiredPoints, bool CanAfford);
