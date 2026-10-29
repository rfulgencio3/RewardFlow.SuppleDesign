namespace RewardFlow.SuppleDesign.Api.DTOs.Wallets;

/// <summary>Original accepted outcome for both sides of a transfer.</summary>
public sealed record RewardTransferResponse(Guid OperationId, Guid SourceWalletId, Guid DestinationWalletId,
    long Points, string Reason, DateTimeOffset OccurredAtUtc, long SourceBalanceAfter, long DestinationBalanceAfter);
