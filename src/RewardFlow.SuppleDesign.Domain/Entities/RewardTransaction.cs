using RewardFlow.SuppleDesign.Domain.ValueObjects;

namespace RewardFlow.SuppleDesign.Domain.Entities;

/// <summary>Conceptual Contours: immutable evidence of one identified wallet movement.</summary>
public sealed record RewardTransaction
{
    /// <summary>Identity supplied by the caller so retries name the same business operation.</summary>
    public Guid OperationId { get; }
    /// <summary>Wallet whose balance was changed.</summary>
    public Guid WalletId { get; }
    /// <summary>Direction of the points movement.</summary>
    public RewardTransactionType Type { get; }
    /// <summary>Positive number of points moved.</summary>
    public RewardPoints Points { get; }
    /// <summary>Business explanation for the movement.</summary>
    public RewardReason Reason { get; }
    /// <summary>UTC instant at which the movement was accepted.</summary>
    public DateTimeOffset OccurredAtUtc { get; }
    /// <summary>Wallet balance immediately after the movement.</summary>
    public RewardPoints BalanceAfter { get; }

    /// <summary>Assertions: creates a complete immutable transaction record.</summary>
    public RewardTransaction(Guid operationId, Guid walletId, RewardTransactionType type, RewardPoints points,
        RewardReason reason, DateTimeOffset occurredAtUtc, RewardPoints balanceAfter)
    {
        DomainAssertions.OperationIdentity(operationId);
        DomainAssertions.WalletIdentity(walletId);
        DomainAssertions.TransactionType(type);
        ArgumentNullException.ThrowIfNull(points);
        DomainAssertions.Positive(points.Value);
        ArgumentNullException.ThrowIfNull(reason);
        DomainAssertions.UtcInstant(occurredAtUtc, "reward_transaction_time_must_be_utc", "A reward transaction time must use UTC.");
        ArgumentNullException.ThrowIfNull(balanceAfter);
        OperationId = operationId;
        WalletId = walletId;
        Type = type;
        Points = points;
        Reason = reason;
        OccurredAtUtc = occurredAtUtc;
        BalanceAfter = balanceAfter;
    }
}

/// <summary>Intention-Revealing Interface: names the two supported reward movements.</summary>
public enum RewardTransactionType
{
    /// <summary>Earned points added to the wallet.</summary>
    Credit = 0,
    /// <summary>Redeemed points removed from the wallet.</summary>
    Debit = 1
}
