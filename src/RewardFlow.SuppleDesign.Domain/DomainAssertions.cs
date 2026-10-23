using RewardFlow.SuppleDesign.Domain.Exceptions;

namespace RewardFlow.SuppleDesign.Domain;

/// <summary>Assertions: explicit preconditions and invariants shared by the domain.</summary>
/// <seealso href="https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html">Eric Evans, Chapter 10: Assertions</seealso>
public static class DomainAssertions
{
    /// <summary>Assertions: point balances must be non-negative.</summary>
    public static void NonNegative(long points)
    {
        if (points < 0)
            throw new DomainException("negative_points", "Reward points cannot be negative.");
    }

    /// <summary>Assertions: a credit or debit must move at least one point.</summary>
    public static void Positive(long points)
    {
        if (points <= 0)
            throw new DomainException("positive_points_required", "A credit or debit must contain at least one reward point.");
    }

    /// <summary>Assertions: zero-balance and insufficient-balance protection prevent overdrafts.</summary>
    public static void SufficientBalance(long balance, long required)
    {
        if (balance < required)
            throw new DomainException("insufficient_points", "The rewards wallet cannot afford the required points.");
    }

    /// <summary>Assertions: every wallet has a meaningful identity.</summary>
    public static void WalletIdentity(Guid id)
    {
        if (id == Guid.Empty)
            throw new DomainException("invalid_wallet_id", "A rewards wallet ID cannot be empty.");
    }

    /// <summary>Assertions: wallet names are normalized, required and bounded.</summary>
    public static string WalletName(string? name, int maximumLength)
    {
        var normalized = name?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new DomainException("wallet_name_required", "A rewards wallet name is required.");
        if (normalized.Length > maximumLength)
            throw new DomainException("wallet_name_too_long", $"A rewards wallet name cannot exceed {maximumLength} characters.");
        return normalized;
    }

    /// <summary>Assertions: creation timestamps are explicit UTC instants.</summary>
    public static void WalletCreationTime(DateTimeOffset createdAtUtc)
    {
        UtcInstant(createdAtUtc, "wallet_creation_time_must_be_utc", "A rewards wallet creation time must use UTC.");
    }

    /// <summary>Assertions: a retried business operation must carry a meaningful identity.</summary>
    public static void OperationIdentity(Guid id)
    {
        if (id == Guid.Empty)
            throw new DomainException("invalid_operation_id", "A reward operation ID cannot be empty.");
    }

    /// <summary>Assertions: timestamps recorded by the domain use UTC.</summary>
    public static void UtcInstant(DateTimeOffset instant, string code, string message)
    {
        if (instant.Offset != TimeSpan.Zero)
            throw new DomainException(code, message);
    }

    /// <summary>Assertions: persisted transaction types must be supported.</summary>
    public static void TransactionType(Entities.RewardTransactionType type)
    {
        if (!Enum.IsDefined(type))
            throw new DomainException("invalid_reward_transaction_type", "The reward transaction type is invalid.");
    }

    /// <summary>Assertions: persisted lifecycle values must name a supported state.</summary>
    public static void WalletStatus(Entities.RewardsWalletStatus status)
    {
        if (!Enum.IsDefined(status))
            throw new DomainException("invalid_wallet_status", "The rewards wallet status is invalid.");
    }
}
