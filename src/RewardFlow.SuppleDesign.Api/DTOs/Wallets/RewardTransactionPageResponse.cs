namespace RewardFlow.SuppleDesign.Api.DTOs.Wallets;

/// <summary>Paged HTTP representation of the immutable wallet ledger.</summary>
public sealed record RewardTransactionPageResponse(
    IReadOnlyList<RewardMovementResponse> Items,
    int Page,
    int PageSize,
    bool HasNextPage);
