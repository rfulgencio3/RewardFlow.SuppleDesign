using RewardFlow.SuppleDesign.Domain.Entities;

namespace RewardFlow.SuppleDesign.Api.DTOs.Wallets;

/// <summary>HTTP representation of an accepted or idempotently replayed reward movement.</summary>
public sealed record RewardMovementResponse(Guid OperationId, RewardTransactionType Type, long Points, string Reason,
    DateTimeOffset OccurredAtUtc, long BalanceAfter);
