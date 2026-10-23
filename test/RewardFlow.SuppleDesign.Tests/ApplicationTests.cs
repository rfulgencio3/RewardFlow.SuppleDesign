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
        Assert.Equal(120, (await new CreditRewardPoints(repository, Clock)
            .ExecuteAsync(id, Guid.NewGuid(), 120, "Purchase reward")).BalanceAfter);
        Assert.Equal(100, (await new DebitRewardPoints(repository, Clock)
            .ExecuteAsync(id, Guid.NewGuid(), 20, "Gift redemption")).BalanceAfter);
        Assert.True((await new CheckWalletAffordability(repository).ExecuteAsync(id, 100)).CanAfford);
        Assert.False((await new CheckWalletAffordability(repository).ExecuteAsync(id, 101)).CanAfford);
        await Assert.ThrowsAsync<DomainException>(() => new DebitRewardPoints(repository, Clock)
            .ExecuteAsync(id, Guid.NewGuid(), 101, "Overspend"));
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
        await new CreditRewardPoints(repository, Clock).ExecuteAsync(id, Guid.NewGuid(), 100, "Purchase reward");

        await new SuspendRewardsWallet(repository).ExecuteAsync(id);
        Assert.Equal(RewardsWalletStatus.Suspended, (await new GetRewardsWallet(repository).ExecuteAsync(id)).Status);
        Assert.Equal(110, (await new CreditRewardPoints(repository, Clock)
            .ExecuteAsync(id, Guid.NewGuid(), 10, "Purchase reward")).BalanceAfter);
        await Assert.ThrowsAsync<DomainException>(() => new DebitRewardPoints(repository, Clock)
            .ExecuteAsync(id, Guid.NewGuid(), 10, "Blocked redemption"));

        await new ReactivateRewardsWallet(repository).ExecuteAsync(id);
        Assert.Equal(100, (await new DebitRewardPoints(repository, Clock)
            .ExecuteAsync(id, Guid.NewGuid(), 10, "Gift redemption")).BalanceAfter);
        await new CloseRewardsWallet(repository).ExecuteAsync(id);
        Assert.Equal(RewardsWalletStatus.Closed, (await new GetRewardsWallet(repository).ExecuteAsync(id)).Status);
        await Assert.ThrowsAsync<DomainException>(() => new CreditRewardPoints(repository, Clock)
            .ExecuteAsync(id, Guid.NewGuid(), 1, "Late purchase"));
    }

    /// <summary>Assertions: retrying the same operation returns its original result without moving points twice.</summary>
    [Fact]
    public async Task RepeatedOperationIsIdempotentAndAppearsOnceInTheLedger()
    {
        var repository = new InMemoryRewardsWalletRepository();
        var id = (await new OpenRewardsWallet(repository, Clock).ExecuteAsync("Idempotent wallet")).WalletId;
        var operationId = Guid.NewGuid();
        var useCase = new CreditRewardPoints(repository, Clock);

        var first = await useCase.ExecuteAsync(id, operationId, 25, "Welcome reward");
        var replay = await useCase.ExecuteAsync(id, operationId, 25, "Welcome reward");
        var ledger = await new ListRewardTransactions(repository).ExecuteAsync(id, 1, 20);

        Assert.Equal(first, replay);
        Assert.Equal(25, (await new GetWalletBalance(repository).ExecuteAsync(id)).Points);
        Assert.Single(ledger.Items);
        await Assert.ThrowsAsync<WalletConflictException>(() =>
            useCase.ExecuteAsync(id, operationId, 30, "Different request"));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
