namespace RewardFlow.SuppleDesign.Infrastructure.Persistence.Models;

/// <summary>Stored original transfer response for deterministic retries.</summary>
internal sealed class RewardTransferState
{
    public Guid OperationId { get; set; }
    public Guid SourceWalletId { get; set; }
    public Guid DestinationWalletId { get; set; }
    public long Points { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public long SourceBalanceAfter { get; set; }
    public long DestinationBalanceAfter { get; set; }
}
