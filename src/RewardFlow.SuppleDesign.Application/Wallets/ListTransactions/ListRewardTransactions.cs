using RewardFlow.SuppleDesign.Domain.Ports;

namespace RewardFlow.SuppleDesign.Application.Wallets;

/// <summary>Intention-Revealing Interface: primary query port for the wallet ledger.</summary>
public interface IListRewardTransactions
{
    /// <summary>Returns a bounded, reverse-chronological page without mutating the wallet.</summary>
    Task<RewardTransactionPage> ExecuteAsync(Guid walletId, int page, int pageSize,
        CancellationToken cancellationToken = default);
}

/// <summary>Side-Effect-Free Function: reads transaction history through the repository port.</summary>
public sealed class ListRewardTransactions(IRewardsWalletRepository repository) : IListRewardTransactions
{
    /// <inheritdoc />
    public async Task<RewardTransactionPage> ExecuteAsync(Guid walletId, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        await WalletPersistence.LoadAsync(repository, walletId, cancellationToken);
        var transactions = await repository.ListTransactionsAsync(walletId, checked((page - 1) * pageSize), pageSize,
            cancellationToken);
        return new(transactions.Select(WalletPersistence.ToResult).ToArray(), page, pageSize);
    }
}
