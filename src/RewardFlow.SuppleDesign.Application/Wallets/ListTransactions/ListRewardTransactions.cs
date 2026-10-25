using RewardFlow.SuppleDesign.Domain.Ports;

namespace RewardFlow.SuppleDesign.Application.Wallets;

/// <summary>Intention-Revealing Interface: primary query port for the wallet ledger.</summary>
public interface IListRewardTransactions
{
    /// <summary>Returns a bounded, reverse-chronological page without mutating the wallet.</summary>
    Task<RewardTransactionPage> ExecuteAsync(Guid walletId, int page, int pageSize,
        CancellationToken cancellationToken = default);
}

/// <summary>Read-only application query: loads transaction history through the repository port without changing wallet state.</summary>
/// <remarks>Performs persistence I/O; immutable point arithmetic, rather than a database read, demonstrates a pure calculation.</remarks>
public sealed class ListRewardTransactions(IRewardsWalletRepository repository) : IListRewardTransactions
{
    /// <inheritdoc />
    public async Task<RewardTransactionPage> ExecuteAsync(Guid walletId, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        await WalletPersistence.LoadAsync(repository, walletId, cancellationToken);
        var transactions = await repository.ListTransactionsAsync(walletId, checked((page - 1) * pageSize), pageSize + 1,
            cancellationToken);
        var hasNextPage = transactions.Count > pageSize;
        return new(transactions.Take(pageSize).Select(WalletPersistence.ToResult).ToArray(), page, pageSize,
            hasNextPage);
    }
}
