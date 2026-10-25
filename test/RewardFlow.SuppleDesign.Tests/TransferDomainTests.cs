using RewardFlow.SuppleDesign.Domain.Entities;
using RewardFlow.SuppleDesign.Domain.Exceptions;
using RewardFlow.SuppleDesign.Domain.Services;
using RewardFlow.SuppleDesign.Domain.ValueObjects;

namespace RewardFlow.SuppleDesign.Tests;

/// <summary>Executable lessons for transfer assertions and calculations without side effects.</summary>
public sealed class TransferDomainTests
{
    private static RewardsWallet Wallet(long balance, RewardsWalletStatus status = RewardsWalletStatus.Active) =>
        new(Guid.NewGuid(), new WalletName("Transfer wallet"), new RewardPoints(balance), status, DateTimeOffset.UtcNow);

    /// <summary>Successful calculations conserve points without changing either input.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void TransferCalculatesBothBalancesWithoutChangingEitherWallet(long points)
    {
        var source = Wallet(100);
        var destination = Wallet(20);
        var before = source.Balance;
        var result = RewardTransferCalculator.Calculate(source, destination, new TransferAmount(points));
        Assert.Equal(100 - points, result.Source.Value);
        Assert.Equal(20 + points, result.Destination.Value);
        Assert.Equal(120, result.Source.Value + result.Destination.Value);
        Assert.Same(before, source.Balance);
        Assert.Equal(20, destination.Balance.Value);
    }

    /// <summary>The transfer limit is independent of ordinary point amounts.</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(long.MaxValue)]
    public void TransferLimitDoesNotChangeOrdinaryPointRules(long points)
    {
        Assert.Equal("invalid_transfer_amount", Assert.Throws<DomainException>(() => new TransferAmount(points)).Code);
        Assert.Equal(1000, new RewardPoints(1000).Value);
    }

    /// <summary>Existing debit and credit lifecycle rules apply to each side.</summary>
    [Theory]
    [InlineData(RewardsWalletStatus.Active, RewardsWalletStatus.Active, true)]
    [InlineData(RewardsWalletStatus.Active, RewardsWalletStatus.Suspended, true)]
    [InlineData(RewardsWalletStatus.Active, RewardsWalletStatus.Closed, false)]
    [InlineData(RewardsWalletStatus.Suspended, RewardsWalletStatus.Active, false)]
    [InlineData(RewardsWalletStatus.Closed, RewardsWalletStatus.Active, false)]
    public void WalletsRetainTheirLifecycleRules(RewardsWalletStatus sourceStatus, RewardsWalletStatus destinationStatus, bool valid)
    {
        var source = Wallet(100, sourceStatus);
        var destination = Wallet(10, destinationStatus);
        if (valid)
            Assert.Equal(110, RewardTransferCalculator.Calculate(source, destination, new TransferAmount(100)).Destination.Value);
        else
            Assert.Throws<DomainException>(() => RewardTransferCalculator.Calculate(source, destination, new TransferAmount(100)));
        Assert.Equal(100, source.Balance.Value);
        Assert.Equal(10, destination.Balance.Value);
    }

    /// <summary>Every rejected calculation preserves both wallet balances.</summary>
    [Fact]
    public void InsufficientBalanceOverflowAndSameWalletPreserveBothInputs()
    {
        var source = Wallet(99);
        var destination = Wallet(long.MaxValue);
        Assert.Equal("insufficient_points", Assert.Throws<DomainException>(() =>
            RewardTransferCalculator.Calculate(source, destination, new TransferAmount(100))).Code);
        Assert.Equal("points_limit_exceeded", Assert.Throws<DomainException>(() =>
            RewardTransferCalculator.Calculate(source, destination, new TransferAmount(1))).Code);
        Assert.Equal("same_wallet_transfer", Assert.Throws<DomainException>(() =>
            RewardTransferCalculator.Calculate(source, source, new TransferAmount(1))).Code);
        Assert.Equal(99, source.Balance.Value);
        Assert.Equal(long.MaxValue, destination.Balance.Value);
    }
}
