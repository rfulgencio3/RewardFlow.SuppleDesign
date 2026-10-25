using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RewardFlow.SuppleDesign.Application.Wallets;
using RewardFlow.SuppleDesign.Domain.Entities;
using RewardFlow.SuppleDesign.Domain.Ports;
using RewardFlow.SuppleDesign.Domain.Services;
using RewardFlow.SuppleDesign.Domain.ValueObjects;
using RewardFlow.SuppleDesign.Infrastructure.Persistence;
using RewardFlow.SuppleDesign.Infrastructure.Repositories;

namespace RewardFlow.SuppleDesign.Tests;

/// <summary>Real PostgreSQL evidence for paired writes, rollback and concurrency.</summary>
[Collection(PostgresDatabaseCollection.Name)]
public sealed class TransferPostgresTests
{
    private static string Connection => Environment.GetEnvironmentVariable("ConnectionStrings__RewardFlow") ??
        "Host=localhost;Port=5432;Database=rewardflow;Username=rewardflow;Password=rewardflow_dev";

    private static RewardFlowDbContext Context(IInterceptor? interceptor = null)
    {
        var options = new DbContextOptionsBuilder<RewardFlowDbContext>()
            .UseNpgsql(Connection, postgres => postgres.MaxBatchSize(1));
        if (interceptor is not null) options.AddInterceptors(interceptor);
        return new(options.Options);
    }

    private static async Task<(Guid Source, Guid Destination)> SeedAsync(RewardFlowDbContext context)
    {
        await context.Database.MigrateAsync();
        var repository = new PostgresRewardsWalletRepository(context);
        var source = Guid.NewGuid();
        var destination = Guid.NewGuid();
        await repository.TryAddAsync(new(source, new WalletName("Source"), new RewardPoints(100), RewardsWalletStatus.Active, DateTimeOffset.UtcNow));
        await repository.TryAddAsync(new(destination, new WalletName("Destination"), DateTimeOffset.UtcNow));
        return (source, destination);
    }

    private static async Task<(RewardTransfer Transfer, long SourceRevision, long DestinationRevision)> PrepareAsync(
        PostgresRewardsWalletRepository repository, Guid sourceId, Guid destinationId, Guid operationId, long points = 80)
    {
        var source = (await repository.FindAsync(sourceId))!;
        var destination = (await repository.FindAsync(destinationId))!;
        var amount = new TransferAmount(points);
        return (new(operationId, sourceId, destinationId, amount, new RewardReason("Transfer"), DateTimeOffset.UtcNow,
            RewardTransferCalculator.Calculate(source.Wallet, destination.Wallet, amount)), source.Revision, destination.Revision);
    }

    /// <summary>Both callers load the same revisions before either save begins; at most one outflow can commit.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentTransfersCannotDoubleSpendOrDuplicateEvidence(bool sameOperation)
    {
        await using var setup = Context();
        var wallets = await SeedAsync(setup);
        await using var firstContext = Context();
        await using var secondContext = Context();
        var first = new PostgresRewardsWalletRepository(firstContext);
        var second = new PostgresRewardsWalletRepository(secondContext);
        var id = Guid.NewGuid();
        var one = await PrepareAsync(first, wallets.Source, wallets.Destination, id);
        var two = await PrepareAsync(second, wallets.Source, wallets.Destination, sameOperation ? id : Guid.NewGuid());
        var results = await Task.WhenAll(
            first.TrySaveTransferAsync(one.Transfer, one.SourceRevision, one.DestinationRevision),
            second.TrySaveTransferAsync(two.Transfer, two.SourceRevision, two.DestinationRevision));
        Assert.Single(results, result => result == MovementSaveResult.Saved);
        Assert.Contains(sameOperation ? MovementSaveResult.DuplicateOperation : MovementSaveResult.RevisionConflict, results);
        await using var readContext = Context();
        var reader = new PostgresRewardsWalletRepository(readContext);
        Assert.Equal(20, (await reader.FindAsync(wallets.Source))!.Wallet.Balance.Value);
        Assert.Equal(80, (await reader.FindAsync(wallets.Destination))!.Wallet.Balance.Value);
        var debit = Assert.Single(await reader.ListTransactionsAsync(wallets.Source, 0, 20));
        var credit = Assert.Single(await reader.ListTransactionsAsync(wallets.Destination, 0, 20));
        Assert.Equal(debit.OperationId, credit.OperationId);
        Assert.Equal(RewardTransactionType.Debit, debit.Type);
        Assert.Equal(RewardTransactionType.Credit, credit.Type);
        var replay = await new TransferRewardPoints(reader, TimeProvider.System)
            .ExecuteAsync(wallets.Source, wallets.Destination, debit.OperationId, 80, "Transfer");
        Assert.Equal(20, replay.SourceBalanceAfter);
    }

    /// <summary>An exception after actual SQL execution must undo balances, revisions, reservation and evidence.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailureAfterBalanceOrLedgerWriteRollsBackEverything(bool failLedger)
    {
        await using var setup = Context();
        var wallets = await SeedAsync(setup);
        var prepared = await PrepareAsync(new(setup), wallets.Source, wallets.Destination, Guid.NewGuid());
        var fault = new WriteFailure(failLedger);
        await using var failing = Context(fault);
        if (failLedger)
        {
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => new PostgresRewardsWalletRepository(failing)
                .TrySaveTransferAsync(prepared.Transfer, prepared.SourceRevision, prepared.DestinationRevision));
            Assert.IsType<InvalidOperationException>(exception.InnerException);
        }
        else
            await Assert.ThrowsAsync<InvalidOperationException>(() => new PostgresRewardsWalletRepository(failing)
                .TrySaveTransferAsync(prepared.Transfer, prepared.SourceRevision, prepared.DestinationRevision));
        Assert.True(fault.Fired);
        await using var verification = Context();
        var reader = new PostgresRewardsWalletRepository(verification);
        Assert.Equal(100, (await reader.FindAsync(wallets.Source))!.Wallet.Balance.Value);
        Assert.Equal(0, (await reader.FindAsync(wallets.Destination))!.Wallet.Balance.Value);
        Assert.Equal(0, (await reader.FindAsync(wallets.Source))!.Revision);
        Assert.Equal(0, (await reader.FindAsync(wallets.Destination))!.Revision);
        Assert.Null(await reader.FindOperationKindAsync(prepared.Transfer.OperationId));
        Assert.Null(await reader.FindTransferAsync(prepared.Transfer.OperationId));
        Assert.Empty(await reader.ListTransactionsAsync(wallets.Source, 0, 20));
        Assert.Empty(await reader.ListTransactionsAsync(wallets.Destination, 0, 20));
    }

    /// <summary>Changing either wallet revision, including lifecycle changes, rejects the entire prepared transfer.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task StaleRevisionOnEitherWalletRejectsTheTransfer(bool changeDestination, bool creditInstead)
    {
        await using var setup = Context();
        var wallets = await SeedAsync(setup);
        var repository = new PostgresRewardsWalletRepository(setup);
        var prepared = await PrepareAsync(repository, wallets.Source, wallets.Destination, Guid.NewGuid());
        var changed = (await repository.FindAsync(changeDestination ? wallets.Destination : wallets.Source))!;
        if (creditInstead)
            await new CreditRewardPoints(repository, TimeProvider.System).ExecuteAsync(changed.Wallet.Id, Guid.NewGuid(), 1, "Concurrent credit");
        else
        {
            changed.Wallet.Suspend();
            Assert.True(await repository.TrySaveAsync(changed.Wallet, changed.Revision));
        }
        Assert.Equal(MovementSaveResult.RevisionConflict, await repository.TrySaveTransferAsync(prepared.Transfer,
            prepared.SourceRevision, prepared.DestinationRevision));
        Assert.Equal(100 + (creditInstead && !changeDestination ? 1 : 0), (await repository.FindAsync(wallets.Source))!.Wallet.Balance.Value);
        Assert.Equal(creditInstead && changeDestination ? 1 : 0, (await repository.FindAsync(wallets.Destination))!.Wallet.Balance.Value);
        Assert.Null(await repository.FindOperationKindAsync(prepared.Transfer.OperationId));
    }

    /// <summary>Opposite directions use the same update order and preserve the combined supply.</summary>
    [Fact]
    public async Task OppositeTransfersCompleteWithoutDeadlockOrLostPoints()
    {
        await using var setup = Context();
        var wallets = await SeedAsync(setup);
        await new CreditRewardPoints(new PostgresRewardsWalletRepository(setup), TimeProvider.System)
            .ExecuteAsync(wallets.Destination, Guid.NewGuid(), 100, "Funding");
        await using var firstContext = Context();
        await using var secondContext = Context();
        var first = new PostgresRewardsWalletRepository(firstContext);
        var second = new PostgresRewardsWalletRepository(secondContext);
        var one = await PrepareAsync(first, wallets.Source, wallets.Destination, Guid.NewGuid(), 30);
        var two = await PrepareAsync(second, wallets.Destination, wallets.Source, Guid.NewGuid(), 20);
        var results = await Task.WhenAll(
            first.TrySaveTransferAsync(one.Transfer, one.SourceRevision, one.DestinationRevision),
            second.TrySaveTransferAsync(two.Transfer, two.SourceRevision, two.DestinationRevision));
        Assert.Single(results, item => item == MovementSaveResult.Saved);
        Assert.Contains(MovementSaveResult.RevisionConflict, results);
        var source = (await first.FindAsync(wallets.Source))!.Wallet.Balance.Value;
        var destination = (await first.FindAsync(wallets.Destination))!.Wallet.Balance.Value;
        Assert.Equal(200, source + destination);
        Assert.Contains(source, new long[] { 70, 120 });
    }

    private sealed class WriteFailure(bool ledger) : DbCommandInterceptor
    {
        public bool Fired { get; private set; }
        private void Check(DbCommand command)
        {
            if (!Fired && (ledger ? command.CommandText.Contains("INSERT INTO reward_transactions", StringComparison.OrdinalIgnoreCase)
                : command.CommandText.Contains("UPDATE rewards_wallets", StringComparison.OrdinalIgnoreCase)))
            {
                Fired = true;
                throw new InvalidOperationException("Injected failure after a persisted transfer write.");
            }
        }

        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            int result, CancellationToken cancellationToken = default)
        {
            Check(command);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            Check(command);
            return ValueTask.FromResult(result);
        }
    }
}
