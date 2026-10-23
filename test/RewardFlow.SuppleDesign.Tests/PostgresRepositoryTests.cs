using Microsoft.EntityFrameworkCore;
using RewardFlow.SuppleDesign.Domain.Entities;
using RewardFlow.SuppleDesign.Domain.Ports;
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

        var openedAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        await using (var setupContext = new RewardFlowDbContext(options))
        {
            await setupContext.Database.MigrateAsync();
            var setupRepository = new PostgresRewardsWalletRepository(setupContext);
            Assert.True(await setupRepository.TryAddAsync(new RewardsWallet(
                walletId,
                new WalletName("PostgreSQL wallet"),
                openedAt)));
        }

        await using var firstContext = new RewardFlowDbContext(options);
        await using var secondContext = new RewardFlowDbContext(options);
        var firstRepository = new PostgresRewardsWalletRepository(firstContext);
        var secondRepository = new PostgresRewardsWalletRepository(secondContext);
        var first = (await firstRepository.FindAsync(walletId))!;
        var stale = (await secondRepository.FindAsync(walletId))!;
        first.Wallet.Credit(new RewardPoints(10));
        first.Wallet.Close();
        stale.Wallet.Credit(new RewardPoints(20));

        Assert.True(await firstRepository.TrySaveAsync(first.Wallet, first.Revision));
        Assert.False(await secondRepository.TrySaveAsync(stale.Wallet, stale.Revision));

        await using var verificationContext = new RewardFlowDbContext(options);
        var saved = await new PostgresRewardsWalletRepository(verificationContext).FindAsync(walletId);
        Assert.Equal(10, saved!.Wallet.Balance.Value);
        Assert.Equal("PostgreSQL wallet", saved.Wallet.Name.Value);
        Assert.Equal(RewardsWalletStatus.Closed, saved.Wallet.Status);
        Assert.Equal(openedAt, saved.Wallet.CreatedAtUtc);
        Assert.Equal(1, saved.Revision);
    }

    /// <summary>Balance and ledger entry are committed together and can be queried through the port.</summary>
    [Fact]
    public async Task MovementPersistsBalanceAndImmutableLedgerAtomically()
    {
        var options = new DbContextOptionsBuilder<RewardFlowDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__RewardFlow") ?? DefaultConnectionString)
            .Options;
        var walletId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var occurredAt = new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);

        await using var context = new RewardFlowDbContext(options);
        await context.Database.MigrateAsync();
        var repository = new PostgresRewardsWalletRepository(context);
        Assert.True(await repository.TryAddAsync(new RewardsWallet(walletId, new WalletName("Ledger wallet"), occurredAt)));
        var snapshot = (await repository.FindAsync(walletId))!;
        snapshot.Wallet.Credit(new RewardPoints(15));
        var movement = new RewardTransaction(operationId, walletId, RewardTransactionType.Credit,
            new RewardPoints(15), new RewardReason("Purchase reward"), occurredAt, snapshot.Wallet.Balance);

        Assert.Equal(MovementSaveResult.Saved,
            await repository.TrySaveMovementAsync(snapshot.Wallet, movement, snapshot.Revision));
        var saved = await repository.FindTransactionAsync(operationId);
        Assert.Equal(movement, saved);
        Assert.Equal(movement, Assert.Single(await repository.ListTransactionsAsync(walletId, 0, 20)));
        Assert.Equal(15, (await repository.FindAsync(walletId))!.Wallet.Balance.Value);
    }
}
