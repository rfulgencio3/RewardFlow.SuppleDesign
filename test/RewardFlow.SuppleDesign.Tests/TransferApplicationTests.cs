using RewardFlow.SuppleDesign.Application.Wallets;
using RewardFlow.SuppleDesign.Domain.Exceptions;
using RewardFlow.SuppleDesign.Tests.TestDoubles;

namespace RewardFlow.SuppleDesign.Tests;

/// <summary>Use-case examples for original-result replay and failed requests.</summary>
public sealed class TransferApplicationTests
{
    private static readonly TimeProvider Clock = new FixedClock();
    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);
    }

    /// <summary>Replay precedes current lifecycle checks and operation identities span every movement kind.</summary>
    [Fact]
    public async Task TransferReplaysOriginalResultAfterClosureAndRejectsCrossEndpointCollisions()
    {
        var repository = new InMemoryRewardsWalletRepository();
        var source = (await new OpenRewardsWallet(repository, Clock).ExecuteAsync("Source")).WalletId;
        var destination = (await new OpenRewardsWallet(repository, Clock).ExecuteAsync("Destination")).WalletId;
        var credit = new CreditRewardPoints(repository, Clock);
        var fundingId = Guid.NewGuid();
        await credit.ExecuteAsync(source, fundingId, 200, "Funding");
        var useCase = new TransferRewardPoints(repository, Clock);
        var id = Guid.NewGuid();
        var first = await useCase.ExecuteAsync(source, destination, id, 100, " Transfer ");
        Assert.Equal(Clock.GetUtcNow(), first.OccurredAtUtc);
        Assert.Equal(100, first.SourceBalanceAfter);
        Assert.Equal(100, first.DestinationBalanceAfter);
        await new CloseRewardsWallet(repository).ExecuteAsync(source);
        await new CloseRewardsWallet(repository).ExecuteAsync(destination);
        Assert.Equal(first, await useCase.ExecuteAsync(source, destination, id, 100, "Transfer"));
        await Assert.ThrowsAsync<WalletConflictException>(() => useCase.ExecuteAsync(source, destination, id, 99, "Transfer"));
        await Assert.ThrowsAsync<WalletConflictException>(() => useCase.ExecuteAsync(destination, source, id, 100, "Transfer"));
        await Assert.ThrowsAsync<WalletConflictException>(() => useCase.ExecuteAsync(source, destination, id, 100, "Changed"));
        await Assert.ThrowsAsync<WalletConflictException>(() => useCase.ExecuteAsync(source, destination, fundingId, 100, "Funding"));
        await Assert.ThrowsAsync<WalletConflictException>(() => credit.ExecuteAsync(source, id, 100, "Transfer"));
        await Assert.ThrowsAsync<WalletConflictException>(() => new DebitRewardPoints(repository, Clock).ExecuteAsync(source, id, 100, "Transfer"));
        Assert.Equal(2, (await new ListRewardTransactions(repository).ExecuteAsync(source, 1, 20)).Items.Count);
        Assert.Single((await new ListRewardTransactions(repository).ExecuteAsync(destination, 1, 20)).Items);
    }

    /// <summary>Invalid inputs, absent wallets and canceled work do not move points.</summary>
    [Fact]
    public async Task InvalidOrMissingWalletsAndCancellationCannotMovePoints()
    {
        var repository = new InMemoryRewardsWalletRepository();
        var source = (await new OpenRewardsWallet(repository, Clock).ExecuteAsync("Source")).WalletId;
        var useCase = new TransferRewardPoints(repository, Clock);
        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(source, Guid.NewGuid(), Guid.NewGuid(), 101, "Transfer"));
        await Assert.ThrowsAsync<WalletNotFoundException>(() => useCase.ExecuteAsync(source, Guid.NewGuid(), Guid.NewGuid(), 1, "Transfer"));
        await Assert.ThrowsAsync<OperationCanceledException>(() => useCase.ExecuteAsync(source, Guid.NewGuid(), Guid.NewGuid(), 1, "Transfer", new CancellationToken(true)));
        Assert.Equal(0, (await new GetWalletBalance(repository).ExecuteAsync(source)).Points);
    }
}
