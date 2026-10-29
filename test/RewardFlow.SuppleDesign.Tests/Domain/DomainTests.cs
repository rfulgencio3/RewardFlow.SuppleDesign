using RewardFlow.SuppleDesign.Domain.Entities;
using RewardFlow.SuppleDesign.Domain.Exceptions;
using RewardFlow.SuppleDesign.Domain.ValueObjects;

namespace RewardFlow.SuppleDesign.Tests.Domain;

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
        // Arrange
        var original = new RewardPoints(100);
        var other = new RewardPoints(25);

        // Act
        var sum = original.Add(other);
        var difference = original.Subtract(other);

        // Assert
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
    public void NegativePointsAreRejected(long points)
    {
        // Arrange
        var create = () => new RewardPoints(points);

        // Act
        var exception = Record.Exception(create);

        // Assert
        Assert.Equal("negative_points", Assert.IsType<DomainException>(exception).Code);
    }

    /// <summary>Assertions: checked business arithmetic prevents overflow and overdrafts.</summary>
    [Fact]
    public void ArithmeticRejectsOverflowAndNegativeResults()
    {
        // Arrange
        var maximum = new RewardPoints(long.MaxValue);
        var one = new RewardPoints(1);

        // Act
        var overflow = Record.Exception(() => maximum.Add(one));
        var underflow = Record.Exception(() => RewardPoints.Zero.Subtract(one));

        // Assert
        Assert.Equal("points_limit_exceeded", Assert.IsType<DomainException>(overflow).Code);
        Assert.Equal("insufficient_points", Assert.IsType<DomainException>(underflow).Code);
    }

    /// <summary>Side-Effect-Free Functions: repeated affordability queries leave the same balance instance intact.</summary>
    [Fact]
    public void CanAffordIsPureAndIncludesTheExactBalance()
    {
        // Arrange
        var wallet = NewWallet(new RewardPoints(100));
        var before = wallet.Balance;

        // Act
        var exact = wallet.CanAfford(new RewardPoints(100));
        var zero = wallet.CanAfford(RewardPoints.Zero);
        var above = wallet.CanAfford(new RewardPoints(101));
        var repeated = wallet.CanAfford(new RewardPoints(101));

        // Assert
        Assert.True(exact);
        Assert.True(zero);
        Assert.False(above);
        Assert.False(repeated);
        Assert.Same(before, wallet.Balance);
    }

    /// <summary>Assertions: overdraft and zero operations leave aggregate state untouched.</summary>
    [Fact]
    public void RejectedOperationsDoNotChangeBalance()
    {
        // Arrange
        var wallet = NewWallet();

        // Act
        var overdraft = Record.Exception(() => wallet.Debit(new RewardPoints(1)));
        var zeroCredit = Record.Exception(() => wallet.Credit(RewardPoints.Zero));
        var emptyBalance = wallet.Balance;
        wallet.Credit(new RewardPoints(long.MaxValue));
        var before = wallet.Balance;
        var overflow = Record.Exception(() => wallet.Credit(new RewardPoints(1)));
        var zeroDebit = Record.Exception(() => wallet.Debit(RewardPoints.Zero));
        var rejectedBalance = wallet.Balance;
        wallet.Debit(new RewardPoints(long.MaxValue));

        // Assert
        Assert.IsType<DomainException>(overdraft);
        Assert.IsType<DomainException>(zeroCredit);
        Assert.Equal(RewardPoints.Zero, emptyBalance);
        Assert.IsType<DomainException>(overflow);
        Assert.IsType<DomainException>(zeroDebit);
        Assert.Same(before, rejectedBalance);
        Assert.Equal(RewardPoints.Zero, wallet.Balance);
    }

    /// <summary>Assertions: empty wallet identity is prohibited at the aggregate boundary.</summary>
    [Fact]
    public void WalletRequiresAnIdentity()
    {
        // Arrange
        var name = new WalletName("Primary rewards");

        // Act
        var exception = Record.Exception(() => new RewardsWallet(Guid.Empty, name, OpenedAt));

        // Assert
        Assert.Equal("invalid_wallet_id", Assert.IsType<DomainException>(exception).Code);
    }

    /// <summary>Standalone Class: wallet names normalize valid input and reject meaningless values.</summary>
    [Fact]
    public void WalletNameOwnsItsNormalizationAndValidity()
    {
        // Arrange
        var oversized = new string('x', WalletName.MaximumLength + 1);

        // Act
        var normalized = new WalletName("  Primary rewards  ");
        var blank = Record.Exception(() => new WalletName("   "));
        var tooLong = Record.Exception(() => new WalletName(oversized));

        // Assert
        Assert.Equal("Primary rewards", normalized.Value);
        Assert.Equal("wallet_name_required", Assert.IsType<DomainException>(blank).Code);
        Assert.Equal("wallet_name_too_long", Assert.IsType<DomainException>(tooLong).Code);
    }

    /// <summary>Assertions: lifecycle transitions protect redemption while preserving earned credits.</summary>
    [Fact]
    public void LifecycleStateGovernsWalletBehavior()
    {
        // Arrange
        var wallet = NewWallet(new RewardPoints(100));

        // Act
        wallet.Suspend();
        wallet.Credit(new RewardPoints(10));
        var suspendedCanAfford = wallet.CanAfford(new RewardPoints(1));
        var blockedDebit = Record.Exception(() => wallet.Debit(new RewardPoints(1)));
        wallet.Reactivate();
        wallet.Debit(new RewardPoints(10));
        wallet.Close();
        var blockedCredit = Record.Exception(() => wallet.Credit(new RewardPoints(1)));
        var blockedReactivation = Record.Exception(wallet.Reactivate);

        // Assert
        Assert.False(suspendedCanAfford);
        Assert.Equal("wallet_not_active", Assert.IsType<DomainException>(blockedDebit).Code);
        Assert.Equal(RewardsWalletStatus.Closed, wallet.Status);
        Assert.Equal("wallet_closed", Assert.IsType<DomainException>(blockedCredit).Code);
        Assert.Equal("invalid_wallet_status_transition", Assert.IsType<DomainException>(blockedReactivation).Code);
    }
}
