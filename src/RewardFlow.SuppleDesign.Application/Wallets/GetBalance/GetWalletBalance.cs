using RewardFlow.SuppleDesign.Domain.Ports;

namespace RewardFlow.SuppleDesign.Application.Wallets;

/// <summary>Query port for obtaining the current wallet balance without requesting a state change.</summary>
public interface IGetWalletBalance
{
    /// <summary>Reads the current balance without creating or changing a wallet.</summary>
    Task<WalletBalance> ExecuteAsync(Guid walletId, CancellationToken cancellationToken = default);
}

/// <summary>Application query that coordinates a balance read through the repository port.</summary>
public sealed class GetWalletBalance(IRewardsWalletRepository repository) : IGetWalletBalance
{
    /// <inheritdoc />
    public async Task<WalletBalance> ExecuteAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var snapshot = await WalletPersistence.LoadAsync(repository, walletId, cancellationToken);
        return new(snapshot.Wallet.Id, snapshot.Wallet.Balance.Value);
    }
}
