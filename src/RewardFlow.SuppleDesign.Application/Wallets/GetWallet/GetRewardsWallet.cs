using RewardFlow.SuppleDesign.Domain.Ports;

namespace RewardFlow.SuppleDesign.Application.Wallets;

/// <summary>Query port for reading the descriptive state of a rewards wallet.</summary>
public interface IGetRewardsWallet
{
    /// <summary>Returns wallet identity, name, lifecycle, creation time and current balance.</summary>
    Task<WalletDetails> ExecuteAsync(Guid walletId, CancellationToken cancellationToken = default);
}

/// <summary>Conceptual Contours: projects aggregate state without exposing the aggregate to HTTP.</summary>
public sealed class GetRewardsWallet(IRewardsWalletRepository repository) : IGetRewardsWallet
{
    /// <inheritdoc />
    public async Task<WalletDetails> ExecuteAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var snapshot = await WalletPersistence.LoadAsync(repository, walletId, cancellationToken);
        var wallet = snapshot.Wallet;
        return new(
            wallet.Id,
            wallet.Name.Value,
            wallet.Status.ToString().ToLowerInvariant(),
            wallet.CreatedAtUtc,
            wallet.Balance.Value);
    }
}
