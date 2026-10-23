using RewardFlow.SuppleDesign.Domain.Ports;
using RewardFlow.SuppleDesign.Domain.ValueObjects;

namespace RewardFlow.SuppleDesign.Domain.Entities;

/// <summary>Conceptual Contours: aggregate state and credit/debit rules live here; point arithmetic lives in RewardPoints.</summary>
/// <seealso href="https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html">Eric Evans, Chapter 10: Conceptual Contours</seealso>
/// <seealso href="https://www.domainlanguage.com/ddd/reference/">Eric Evans' DDD Reference: Entities and Aggregates</seealso>
public sealed class RewardsWallet : IRewardsWallet
{
    /// <inheritdoc />
    public Guid Id { get; }
    /// <inheritdoc />
    public WalletName Name { get; }
    /// <inheritdoc />
    public RewardsWalletStatus Status { get; private set; }
    /// <inheritdoc />
    public DateTimeOffset CreatedAtUtc { get; }
    /// <inheritdoc />
    public RewardPoints Balance { get; private set; }

    /// <summary>Assertions: opens a named, active wallet with a zero balance at an explicit UTC instant.</summary>
    public RewardsWallet(Guid id, WalletName name, DateTimeOffset createdAtUtc)
        : this(id, name, RewardPoints.Zero, RewardsWalletStatus.Active, createdAtUtc) { }

    /// <summary>Assertions: reconstitutes a wallet from valid immutable values and lifecycle state.</summary>
    public RewardsWallet(
        Guid id,
        WalletName name,
        RewardPoints balance,
        RewardsWalletStatus status,
        DateTimeOffset createdAtUtc)
    {
        DomainAssertions.WalletIdentity(id);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(balance);
        DomainAssertions.WalletStatus(status);
        DomainAssertions.WalletCreationTime(createdAtUtc);
        Id = id;
        Name = name;
        Balance = balance;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    /// <inheritdoc />
    public void Credit(RewardPoints earnedPoints)
    {
        ArgumentNullException.ThrowIfNull(earnedPoints);
        EnsureNotClosed("credit");
        DomainAssertions.Positive(earnedPoints.Value);
        Balance = Balance.Add(earnedPoints);
    }

    /// <inheritdoc />
    public void Debit(RewardPoints redeemedPoints)
    {
        ArgumentNullException.ThrowIfNull(redeemedPoints);
        EnsureActive("debit");
        DomainAssertions.Positive(redeemedPoints.Value);
        Balance = Balance.Subtract(redeemedPoints);
    }

    /// <inheritdoc />
    public bool CanAfford(RewardPoints requiredPoints)
    {
        ArgumentNullException.ThrowIfNull(requiredPoints);
        return Status == RewardsWalletStatus.Active && Balance.Value >= requiredPoints.Value;
    }

    /// <inheritdoc />
    public void Suspend()
    {
        EnsureStatus(RewardsWalletStatus.Active, "suspend");
        Status = RewardsWalletStatus.Suspended;
    }

    /// <inheritdoc />
    public void Reactivate()
    {
        EnsureStatus(RewardsWalletStatus.Suspended, "reactivate");
        Status = RewardsWalletStatus.Active;
    }

    /// <inheritdoc />
    public void Close()
    {
        if (Status == RewardsWalletStatus.Closed)
            throw new Exceptions.DomainException(
                "invalid_wallet_status_transition",
                "A closed rewards wallet cannot be closed again.");
        Status = RewardsWalletStatus.Closed;
    }

    private void EnsureActive(string operation)
    {
        if (Status != RewardsWalletStatus.Active)
            throw new Exceptions.DomainException(
                "wallet_not_active",
                $"Only an active rewards wallet can {operation} reward points.");
    }

    private void EnsureNotClosed(string operation)
    {
        if (Status == RewardsWalletStatus.Closed)
            throw new Exceptions.DomainException(
                "wallet_closed",
                $"A closed rewards wallet cannot {operation} reward points.");
    }

    private void EnsureStatus(RewardsWalletStatus required, string transition)
    {
        if (Status != required)
            throw new Exceptions.DomainException(
                "invalid_wallet_status_transition",
                $"A rewards wallet in status {Status} cannot {transition}.");
    }
}
