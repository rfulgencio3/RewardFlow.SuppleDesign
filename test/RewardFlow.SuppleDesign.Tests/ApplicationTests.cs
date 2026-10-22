using RewardFlow.SuppleDesign.Application.Wallets;
using RewardFlow.SuppleDesign.Domain.Exceptions;
using RewardFlow.SuppleDesign.Domain.ValueObjects;
using RewardFlow.SuppleDesign.Infrastructure.Repositories;

namespace RewardFlow.SuppleDesign.Tests;

/// <summary>Executable examples of application ports and the persistence adapter's isolation contract.</summary>
public sealed class ApplicationTests
{
    /// <summary>Conceptual Contours: commands coordinate persisted wallet behavior.</summary>
    [Fact]
    public async Task OpenCreditDebitAndReadCoordinateTheAggregate()
    {
        var repository = new InMemoryRewardsWalletRepository();
        var id = Guid.NewGuid();
        Assert.Equal(0, (await new OpenRewardsWallet(repository).ExecuteAsync(id)).Points);
        Assert.Equal(120, (await new CreditRewardPoints(repository).ExecuteAsync(id, 120)).Points);
        Assert.Equal(100, (await new DebitRewardPoints(repository).ExecuteAsync(id, 20)).Points);
        await Assert.ThrowsAsync<DomainException>(() => new DebitRewardPoints(repository).ExecuteAsync(id, 101));
        Assert.Equal(100, (await new GetWalletBalance(repository).ExecuteAsync(id)).Points);
        await Assert.ThrowsAsync<WalletConflictException>(() => new OpenRewardsWallet(repository).ExecuteAsync(id));
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
        var id = Guid.NewGuid();
        await new OpenRewardsWallet(repository).ExecuteAsync(id);
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
        var id = Guid.NewGuid();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new OpenRewardsWallet(repository).ExecuteAsync(id, new CancellationToken(true)));
        Assert.Null(await repository.FindAsync(id));
    }
}
