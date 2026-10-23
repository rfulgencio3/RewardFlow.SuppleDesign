using RewardFlow.SuppleDesign.Application.Wallets;
using RewardFlow.SuppleDesign.Domain.Entities;
using RewardFlow.SuppleDesign.Domain.Exceptions;
using RewardFlow.SuppleDesign.Domain.ValueObjects;
using RewardFlow.SuppleDesign.Tests.TestDoubles;

namespace RewardFlow.SuppleDesign.Tests;

/// <summary>Executable examples of application ports and the persistence adapter's isolation contract.</summary>
public sealed class ApplicationTests
{
    private static readonly DateTimeOffset OpenedAt = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private static readonly TimeProvider Clock = new FixedTimeProvider(OpenedAt);

    /// <summary>Conceptual Contours: commands coordinate persisted wallet behavior.</summary>
    [Fact]
    public async Task OpenCreditDebitAndReadCoordinateTheAggregate()
    {
        var repository = new InMemoryRewardsWalletRepository();
        var opened = await new OpenRewardsWallet(repository, Clock).ExecuteAsync(" Primary rewards ");
        var id = opened.WalletId;
        var details = await new GetRewardsWallet(repository).ExecuteAsync(id);
        Assert.Equal("Primary rewards", details.Name);
        Assert.Equal(RewardsWalletStatus.Active, details.Status);
        Assert.Equal(OpenedAt, details.CreatedAtUtc);
        Assert.Equal(0, details.Points);
        Assert.Equal(120, (await new CreditRewardPoints(repository).ExecuteAsync(id, 120)).Points);
        Assert.Equal(100, (await new DebitRewardPoints(repository).ExecuteAsync(id, 20)).Points);
        Assert.True((await new CheckWalletAffordability(repository).ExecuteAsync(id, 100)).CanAfford);
        Assert.False((await new CheckWalletAffordability(repository).ExecuteAsync(id, 101)).CanAfford);
        await Assert.ThrowsAsync<DomainException>(() => new DebitRewardPoints(repository).ExecuteAsync(id, 101));
        Assert.Equal(100, (await new GetWalletBalance(repository).ExecuteAsync(id)).Points);
    }

    /// <summary>Side-Effect-Free Functions: a missing-wallet query never implicitly creates a wallet.</summary>
    [Fact]
    public async Task MissingWalletRemainsMissing()
    {
        var repository = new InMemoryRewardsWalletRepository();
        var id = Guid.NewGuid();
        await Assert.ThrowsAsync<WalletNotFoundException>(() => new GetWalletBalance(repository).ExecuteAsync(id));
        Assert.Null(await repository.FindAsync(id));
    }

    /// <summary>Assertions: stale saves fail and detached mutations cannot leak into persistence.</summary>
    [Fact]
    public async Task StaleSnapshotsCannotOverwriteSavedCredits()
    {
        var repository = new InMemoryRewardsWalletRepository();
        var id = (await new OpenRewardsWallet(repository, Clock).ExecuteAsync("Concurrent wallet")).WalletId;
        var first = (await repository.FindAsync(id))!;
        var second = (await repository.FindAsync(id))!;
        first.Wallet.Credit(new RewardPoints(10));
        Assert.Equal(0, (await repository.FindAsync(id))!.Wallet.Balance.Value);
        second.Wallet.Credit(new RewardPoints(20));
        Assert.True(await repository.TrySaveAsync(first.Wallet, first.Revision));
        Assert.False(await repository.TrySaveAsync(second.Wallet, second.Revision));
        Assert.Equal(10, (await repository.FindAsync(id))!.Wallet.Balance.Value);
    }

    /// <summary>Assertions: cancellation is honored before creating persisted state.</summary>
    [Fact]
    public async Task CancelledOpenDoesNotPersist()
    {
        var repository = new InMemoryRewardsWalletRepository();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new OpenRewardsWallet(repository, Clock).ExecuteAsync("Cancelled wallet", new CancellationToken(true)));
    }

    /// <summary>Intention-Revealing Interfaces: lifecycle use cases delegate state rules to the aggregate.</summary>
    [Fact]
    public async Task SuspendReactivateAndCloseCoordinateLifecycle()
    {
        var repository = new InMemoryRewardsWalletRepository();
        var id = (await new OpenRewardsWallet(repository, Clock).ExecuteAsync("Lifecycle wallet")).WalletId;
        await new CreditRewardPoints(repository).ExecuteAsync(id, 100);

        await new SuspendRewardsWallet(repository).ExecuteAsync(id);
        Assert.Equal(RewardsWalletStatus.Suspended, (await new GetRewardsWallet(repository).ExecuteAsync(id)).Status);
        Assert.Equal(110, (await new CreditRewardPoints(repository).ExecuteAsync(id, 10)).Points);
        await Assert.ThrowsAsync<DomainException>(() => new DebitRewardPoints(repository).ExecuteAsync(id, 10));

        await new ReactivateRewardsWallet(repository).ExecuteAsync(id);
        Assert.Equal(100, (await new DebitRewardPoints(repository).ExecuteAsync(id, 10)).Points);
        await new CloseRewardsWallet(repository).ExecuteAsync(id);
        Assert.Equal(RewardsWalletStatus.Closed, (await new GetRewardsWallet(repository).ExecuteAsync(id)).Status);
        await Assert.ThrowsAsync<DomainException>(() => new CreditRewardPoints(repository).ExecuteAsync(id, 1));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
