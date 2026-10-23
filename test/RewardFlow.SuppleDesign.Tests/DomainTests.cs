using RewardFlow.SuppleDesign.Domain.Entities;
using RewardFlow.SuppleDesign.Domain.Exceptions;
using RewardFlow.SuppleDesign.Domain.ValueObjects;

namespace RewardFlow.SuppleDesign.Tests;

/// <summary>Executable examples of Assertions, Closure of Operations and Side-Effect-Free Functions.</summary>
public sealed class DomainTests
{
    private static readonly DateTimeOffset OpenedAt = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    private static RewardsWallet NewWallet(RewardPoints? balance = null) =>
        new(
            Guid.NewGuid(),
            new WalletName("Primary rewards"),
            balance ?? RewardPoints.Zero,
            RewardsWalletStatus.Active,
            OpenedAt);

    /// <summary>Closure of Operations: immutable arithmetic remains inside the RewardPoints type.</summary>
    [Fact]
    public void ArithmeticReturnsNewValuesAndPreservesOperands()
    {
        var original = new RewardPoints(100);
        var other = new RewardPoints(25);
        var sum = original.Add(other);
        var difference = original.Subtract(other);
        Assert.Equal(new RewardPoints(125), sum);
        Assert.Equal(new RewardPoints(75), difference);
        Assert.NotSame(original, sum);
        Assert.NotSame(original, difference);
        Assert.Equal(100, original.Value);
        Assert.Equal(25, other.Value);
    }

    /// <summary>Assertions: invalid point values are impossible to construct.</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(long.MinValue)]
    public void NegativePointsAreRejected(long points) =>
        Assert.Equal("negative_points", Assert.Throws<DomainException>(() => new RewardPoints(points)).Code);

    /// <summary>Assertions: checked business arithmetic prevents overflow and overdrafts.</summary>
    [Fact]
    public void ArithmeticRejectsOverflowAndNegativeResults()
    {
        Assert.Equal("points_limit_exceeded", Assert.Throws<DomainException>(() =>
            new RewardPoints(long.MaxValue).Add(new RewardPoints(1))).Code);
        Assert.Equal("insufficient_points", Assert.Throws<DomainException>(() =>
            RewardPoints.Zero.Subtract(new RewardPoints(1))).Code);
    }

    /// <summary>Side-Effect-Free Functions: repeated affordability queries leave the same balance instance intact.</summary>
    [Fact]
    public void CanAffordIsPureAndIncludesTheExactBalance()
    {
        var wallet = NewWallet(new RewardPoints(100));
        var before = wallet.Balance;
        Assert.True(wallet.CanAfford(new RewardPoints(100)));
        Assert.True(wallet.CanAfford(RewardPoints.Zero));
        Assert.False(wallet.CanAfford(new RewardPoints(101)));
        Assert.False(wallet.CanAfford(new RewardPoints(101)));
        Assert.Same(before, wallet.Balance);
    }

    /// <summary>Assertions: overdraft and zero operations leave aggregate state untouched.</summary>
    [Fact]
    public void RejectedOperationsDoNotChangeBalance()
    {
        var wallet = NewWallet();
        Assert.Throws<DomainException>(() => wallet.Debit(new RewardPoints(1)));
        Assert.Throws<DomainException>(() => wallet.Credit(RewardPoints.Zero));
        wallet.Credit(new RewardPoints(long.MaxValue));
        var before = wallet.Balance;
        Assert.Throws<DomainException>(() => wallet.Credit(new RewardPoints(1)));
        Assert.Throws<DomainException>(() => wallet.Debit(RewardPoints.Zero));
        Assert.Same(before, wallet.Balance);
        wallet.Debit(new RewardPoints(long.MaxValue));
        Assert.Equal(RewardPoints.Zero, wallet.Balance);
    }

    /// <summary>Assertions: empty wallet identity is prohibited at the aggregate boundary.</summary>
    [Fact]
    public void WalletRequiresAnIdentity() =>
        Assert.Equal("invalid_wallet_id", Assert.Throws<DomainException>(() =>
            new RewardsWallet(Guid.Empty, new WalletName("Primary rewards"), OpenedAt)).Code);

    /// <summary>Standalone Class: wallet names normalize valid input and reject meaningless values.</summary>
    [Fact]
    public void WalletNameOwnsItsNormalizationAndValidity()
    {
        Assert.Equal("Primary rewards", new WalletName("  Primary rewards  ").Value);
        Assert.Equal("wallet_name_required",
            Assert.Throws<DomainException>(() => new WalletName("   ")).Code);
        Assert.Equal("wallet_name_too_long",
            Assert.Throws<DomainException>(() => new WalletName(new string('x', WalletName.MaximumLength + 1))).Code);
    }

    /// <summary>Assertions: lifecycle transitions protect redemption while preserving earned credits.</summary>
    [Fact]
    public void LifecycleStateGovernsWalletBehavior()
    {
        var wallet = NewWallet(new RewardPoints(100));
        wallet.Suspend();
        wallet.Credit(new RewardPoints(10));
        Assert.False(wallet.CanAfford(new RewardPoints(1)));
        Assert.Equal("wallet_not_active",
            Assert.Throws<DomainException>(() => wallet.Debit(new RewardPoints(1))).Code);

        wallet.Reactivate();
        wallet.Debit(new RewardPoints(10));
        wallet.Close();
        Assert.Equal(RewardsWalletStatus.Closed, wallet.Status);
        Assert.Equal("wallet_closed",
            Assert.Throws<DomainException>(() => wallet.Credit(new RewardPoints(1))).Code);
        Assert.Equal("invalid_wallet_status_transition",
            Assert.Throws<DomainException>(wallet.Reactivate).Code);
    }
}
