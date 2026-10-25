using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using RewardFlow.SuppleDesign.Application.Wallets;
using RewardFlow.SuppleDesign.Infrastructure.Persistence;
using RewardFlow.SuppleDesign.Infrastructure.Repositories;

namespace RewardFlow.SuppleDesign.Tests;

/// <summary>Isolated databases demonstrate fresh installation, historical replay and safe downgrade behavior.</summary>
[Collection(PostgresDatabaseCollection.Name)]
public sealed class TransferMigrationTests
{
    /// <summary>Backfill preserves old IDs/results, downgrade preserves simple entries and refuses transfer loss.</summary>
    [Fact]
    public async Task UpgradePreservesHistoryAndDowngradeCannotDiscardTransfers()
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__RewardFlow") ??
            "Host=localhost;Port=5432;Database=rewardflow;Username=rewardflow;Password=rewardflow_dev";
        var database = "rewardflow_transfer_test_" + Guid.NewGuid().ToString("N");
        var adminBuilder = new NpgsqlConnectionStringBuilder(connection) { Database = "postgres", Pooling = false };
        await using var admin = new NpgsqlConnection(adminBuilder.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin))
            await create.ExecuteNonQueryAsync();
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connection) { Database = database, Pooling = false };
            var options = new DbContextOptionsBuilder<RewardFlowDbContext>().UseNpgsql(builder.ConnectionString).Options;
            await using var context = new RewardFlowDbContext(options);
            var migrator = context.GetService<IMigrator>();
            const string previous = "20261023184620_AddRewardTransactionLedger";
            // Exercise a fresh installation first, then downgrade without transfers to seed the old schema.
            await migrator.MigrateAsync();
            Assert.True(await context.Database.SqlQueryRaw<bool>("""
                SELECT column_default IS NULL AS "Value"
                FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'reward_transactions' AND column_name = 'transaction_id'
                """).SingleAsync());
            await migrator.MigrateAsync(previous);
            var source = Guid.NewGuid();
            var destination = Guid.NewGuid();
            var operation = Guid.NewGuid();
            var instant = new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO rewards_wallets (id, name, status, created_at_utc, points, revision)
                VALUES ({source}, 'Source', 0, {instant}, 100, 1), ({destination}, 'Destination', 0, {instant}, 0, 0);
                INSERT INTO reward_transactions (operation_id, wallet_id, type, points, reason, occurred_at_utc, balance_after)
                VALUES ({operation}, {source}, 0, 100, 'Funding', {instant}, 100);
                """);
            await migrator.MigrateAsync();
            var repository = new PostgresRewardsWalletRepository(context);
            var replay = await new CreditRewardPoints(repository, TimeProvider.System).ExecuteAsync(source, operation, 100, "Funding");
            Assert.Equal(instant, replay.OccurredAtUtc);
            Assert.Equal(100, replay.BalanceAfter);
            Assert.Equal(1, (await repository.FindAsync(source))!.Revision);
            Assert.Equal(operation, Assert.Single(await repository.ListTransactionsAsync(source, 0, 20)).OperationId);
            await migrator.MigrateAsync(previous);
            await migrator.MigrateAsync();
            Assert.Equal(replay, await new CreditRewardPoints(repository, TimeProvider.System).ExecuteAsync(source, operation, 100, "Funding"));
            await new TransferRewardPoints(repository, TimeProvider.System).ExecuteAsync(source, destination, Guid.NewGuid(), 100, "Transfer");
            var failure = await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync(previous));
            Assert.Contains("Cannot downgrade AddWalletTransfers", failure.MessageText);
            Assert.Equal(100, (await repository.FindAsync(destination))!.Wallet.Balance.Value);
            Assert.Single(await repository.ListTransactionsAsync(destination, 0, 20));
        }
        finally
        {
            // This database name is generated exclusively for this test and never names a user database.
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
