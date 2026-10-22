using RewardFlow.SuppleDesign.Domain.Ports;
using RewardFlow.SuppleDesign.Domain.ValueObjects;

namespace RewardFlow.SuppleDesign.Application.Wallets;

/// <summary>Query port for checking a prospective redemption without changing wallet state.</summary>
public interface ICheckWalletAffordability
{
    /// <summary>Checks whether the wallet can afford the required points.</summary>
    Task<WalletAffordability> ExecuteAsync(
        Guid walletId,
        long requiredPoints,
        CancellationToken cancellationToken = default);
}

/// <summary>Application query that exposes the aggregate's side-effect-free affordability function.</summary>
public sealed class CheckWalletAffordability(IRewardsWalletRepository repository) : ICheckWalletAffordability
{
    /// <inheritdoc />
    public async Task<WalletAffordability> ExecuteAsync(
        Guid walletId,
        long requiredPoints,
        CancellationToken cancellationToken = default)
    {
        var required = new RewardPoints(requiredPoints);
        var snapshot = await WalletPersistence.LoadAsync(repository, walletId, cancellationToken);

        return new(
            snapshot.Wallet.Id,
            snapshot.Wallet.Balance.Value,
            required.Value,
            snapshot.Wallet.CanAfford(required));
    }
}
