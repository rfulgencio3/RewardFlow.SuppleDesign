using RewardFlow.SuppleDesign.Domain.Entities;

namespace RewardFlow.SuppleDesign.Application.Wallets;

/// <summary>Intention-Revealing Interfaces: application result independent of HTTP contracts.</summary>
/// <remarks>Keeps use cases reusable without making the application layer depend on API DTOs.</remarks>
public sealed record WalletBalance(long Points);

/// <summary>Result returned after the application opens a wallet with generated identity.</summary>
public sealed record WalletOpened(Guid WalletId);

/// <summary>Query result describing the wallet without exposing the mutable aggregate.</summary>
public sealed record WalletDetails(
    Guid WalletId,
    string Name,
    RewardsWalletStatus Status,
    DateTimeOffset CreatedAtUtc,
    long Points);

/// <summary>Query result describing whether a wallet can fund a prospective redemption.</summary>
/// <remarks>Keeps the query result independent of its current HTTP representation.</remarks>
public sealed record WalletAffordability(long AvailablePoints, long RequiredPoints, bool CanAfford);

/// <summary>Immutable result of an accepted or idempotently replayed reward movement.</summary>
public sealed record RewardMovement(Guid OperationId, RewardTransactionType Type, long Points, string Reason,
    DateTimeOffset OccurredAtUtc, long BalanceAfter);

/// <summary>Read model for one page of the wallet's immutable transaction ledger.</summary>
public sealed record RewardTransactionPage(IReadOnlyList<RewardMovement> Items, int Page, int PageSize);
