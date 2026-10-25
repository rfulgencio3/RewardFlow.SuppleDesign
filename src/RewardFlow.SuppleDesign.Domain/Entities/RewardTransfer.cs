using RewardFlow.SuppleDesign.Domain.Services;
using RewardFlow.SuppleDesign.Domain.ValueObjects;

namespace RewardFlow.SuppleDesign.Domain.Entities;

/// <summary>Immutable evidence of one accepted transfer, including the original result for replay.</summary>
public sealed record RewardTransfer
{
    /// <summary>Global idempotency identity.</summary>
    public Guid OperationId { get; }
    /// <summary>Wallet providing the points.</summary>
    public Guid SourceWalletId { get; }
    /// <summary>Wallet receiving the points.</summary>
    public Guid DestinationWalletId { get; }
    /// <summary>Transfer-specific validated quantity.</summary>
    public TransferAmount Amount { get; }
    /// <summary>Normalized business explanation.</summary>
    public RewardReason Reason { get; }
    /// <summary>Explicit UTC acceptance time.</summary>
    public DateTimeOffset OccurredAtUtc { get; }
    /// <summary>Original resulting balances, independent of later wallet changes.</summary>
    public TransferBalances Balances { get; }

    /// <summary>Assertions: constructs valid immutable transfer evidence.</summary>
    public RewardTransfer(Guid operationId, Guid sourceWalletId, Guid destinationWalletId,
        TransferAmount amount, RewardReason reason, DateTimeOffset occurredAtUtc, TransferBalances balances)
    {
        DomainAssertions.OperationIdentity(operationId);
        DomainAssertions.WalletIdentity(sourceWalletId);
        DomainAssertions.WalletIdentity(destinationWalletId);
        if (sourceWalletId == destinationWalletId)
            throw new Exceptions.DomainException("same_wallet_transfer", "A transfer requires two different rewards wallets.");
        ArgumentNullException.ThrowIfNull(amount);
        ArgumentNullException.ThrowIfNull(reason);
        ArgumentNullException.ThrowIfNull(balances);
        ArgumentNullException.ThrowIfNull(balances.Source);
        ArgumentNullException.ThrowIfNull(balances.Destination);
        DomainAssertions.UtcInstant(occurredAtUtc, "reward_transaction_time_must_be_utc", "A reward transaction time must use UTC.");
        OperationId = operationId;
        SourceWalletId = sourceWalletId;
        DestinationWalletId = destinationWalletId;
        Amount = amount;
        Reason = reason;
        OccurredAtUtc = occurredAtUtc;
        Balances = balances;
    }
}

/// <summary>Stable persisted operation kinds; a transfer has two ledger entries, not two operations.</summary>
public enum RewardOperationKind
{
    /// <summary>An ordinary earned-points credit.</summary>
    Credit = 0,
    /// <summary>An ordinary redemption.</summary>
    Debit = 1,
    /// <summary>A paired wallet transfer.</summary>
    Transfer = 2
}
