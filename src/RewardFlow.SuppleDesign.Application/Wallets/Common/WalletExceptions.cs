namespace RewardFlow.SuppleDesign.Application.Wallets;

/// <summary>Intention-Revealing Interfaces: an application failure understood by primary adapters.</summary>
public sealed class WalletNotFoundException(Guid walletId) : Exception($"Rewards wallet '{walletId}' was not found.");

/// <summary>Assertions: signals a duplicate identity or a concurrent write that the caller can retry.</summary>
public sealed class WalletConflictException(string message) : Exception(message);
