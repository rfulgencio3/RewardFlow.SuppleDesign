using RewardFlow.SuppleDesign.Domain.Entities;

namespace RewardFlow.SuppleDesign.Infrastructure.Persistence.Models;

/// <summary>Persistence projection of an immutable reward transaction.</summary>
internal sealed class RewardTransactionState
{
    public Guid OperationId { get; set; }
    public Guid WalletId { get; set; }
    public RewardTransactionType Type { get; set; }
    public long Points { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public long BalanceAfter { get; set; }
    public RewardsWalletState Wallet { get; set; } = null!;
}
