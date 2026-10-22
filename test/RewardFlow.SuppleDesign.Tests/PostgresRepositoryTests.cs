using Microsoft.EntityFrameworkCore;
using RewardFlow.SuppleDesign.Domain.Entities;
using RewardFlow.SuppleDesign.Domain.ValueObjects;
using RewardFlow.SuppleDesign.Infrastructure.Persistence;
using RewardFlow.SuppleDesign.Infrastructure.Repositories;

namespace RewardFlow.SuppleDesign.Tests;

/// <summary>Verifies that the PostgreSQL adapter honors the repository port's concurrency contract.</summary>
[Collection(PostgresDatabaseCollection.Name)]
public sealed class PostgresRepositoryTests
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=rewardflow;Username=rewardflow;Password=rewardflow_dev";

    /// <summary>A stale detached snapshot cannot overwrite a wallet saved from the same revision.</summary>
    [Fact]
    public async Task StaleSnapshotCannotOverwritePersistedBalance()
    {
        var options = new DbContextOptionsBuilder<RewardFlowDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__RewardFlow") ?? DefaultConnectionString)
            .Options;
        var walletId = Guid.NewGuid();

        await using (var setupContext = new RewardFlowDbContext(options))
        {
            await setupContext.Database.MigrateAsync();
            var setupRepository = new PostgresRewardsWalletRepository(setupContext);
            Assert.True(await setupRepository.TryAddAsync(new RewardsWallet(walletId)));
        }

        await using var firstContext = new RewardFlowDbContext(options);
        await using var secondContext = new RewardFlowDbContext(options);
        var firstRepository = new PostgresRewardsWalletRepository(firstContext);
        var secondRepository = new PostgresRewardsWalletRepository(secondContext);
        var first = (await firstRepository.FindAsync(walletId))!;
        var stale = (await secondRepository.FindAsync(walletId))!;
        first.Wallet.Credit(new RewardPoints(10));
        stale.Wallet.Credit(new RewardPoints(20));

        Assert.True(await firstRepository.TrySaveAsync(first.Wallet, first.Revision));
        Assert.False(await secondRepository.TrySaveAsync(stale.Wallet, stale.Revision));

        await using var verificationContext = new RewardFlowDbContext(options);
        var saved = await new PostgresRewardsWalletRepository(verificationContext).FindAsync(walletId);
        Assert.Equal(10, saved!.Wallet.Balance.Value);
        Assert.Equal(1, saved.Revision);
    }
}
