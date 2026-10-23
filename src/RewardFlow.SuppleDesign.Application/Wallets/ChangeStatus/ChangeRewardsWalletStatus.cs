using RewardFlow.SuppleDesign.Domain.Ports;

namespace RewardFlow.SuppleDesign.Application.Wallets;

/// <summary>Intention-Revealing Interfaces: primary port for temporarily suspending a wallet.</summary>
public interface ISuspendRewardsWallet
{
    /// <summary>Suspends an active wallet and persists the transition.</summary>
    Task ExecuteAsync(Guid walletId, CancellationToken cancellationToken = default);
}

/// <summary>Coordinates suspension while the aggregate owns transition rules.</summary>
public sealed class SuspendRewardsWallet(IRewardsWalletRepository repository) : ISuspendRewardsWallet
{
    /// <inheritdoc />
    public async Task ExecuteAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var snapshot = await WalletPersistence.LoadAsync(repository, walletId, cancellationToken);
        snapshot.Wallet.Suspend();
        await WalletPersistence.SaveAsync(repository, snapshot, cancellationToken);
    }
}

/// <summary>Intention-Revealing Interfaces: primary port for reactivating a suspended wallet.</summary>
public interface IReactivateRewardsWallet
{
    /// <summary>Reactivates a suspended wallet and persists the transition.</summary>
    Task ExecuteAsync(Guid walletId, CancellationToken cancellationToken = default);
}

/// <summary>Coordinates reactivation while the aggregate owns transition rules.</summary>
public sealed class ReactivateRewardsWallet(IRewardsWalletRepository repository) : IReactivateRewardsWallet
{
    /// <inheritdoc />
    public async Task ExecuteAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var snapshot = await WalletPersistence.LoadAsync(repository, walletId, cancellationToken);
        snapshot.Wallet.Reactivate();
        await WalletPersistence.SaveAsync(repository, snapshot, cancellationToken);
    }
}

/// <summary>Intention-Revealing Interfaces: primary port for permanently closing a wallet.</summary>
public interface ICloseRewardsWallet
{
    /// <summary>Closes a wallet and persists its terminal state.</summary>
    Task ExecuteAsync(Guid walletId, CancellationToken cancellationToken = default);
}

/// <summary>Coordinates closure while the aggregate owns transition rules.</summary>
public sealed class CloseRewardsWallet(IRewardsWalletRepository repository) : ICloseRewardsWallet
{
    /// <inheritdoc />
    public async Task ExecuteAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var snapshot = await WalletPersistence.LoadAsync(repository, walletId, cancellationToken);
        snapshot.Wallet.Close();
        await WalletPersistence.SaveAsync(repository, snapshot, cancellationToken);
    }
}
