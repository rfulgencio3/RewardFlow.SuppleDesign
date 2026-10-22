using RewardFlow.SuppleDesign.Domain;
using RewardFlow.SuppleDesign.Domain.Ports;

namespace RewardFlow.SuppleDesign.Application.Wallets;

/// <summary>Conceptual Contours: shared application persistence coordination.</summary>
/// <remarks>
/// This helper exists because multiple use cases share the same not-found and optimistic-concurrency policy. It owns
/// no business rule and should be removed if that coordination stops being shared.
/// </remarks>
internal static class WalletPersistence
{
    /// <summary>Assertions: a command or query must address an existing, identified wallet.</summary>
    internal static async Task<WalletSnapshot> LoadAsync(
        IRewardsWalletRepository repository,
        Guid walletId,
        CancellationToken cancellationToken)
    {
        DomainAssertions.WalletIdentity(walletId);
        return await repository.FindAsync(walletId, cancellationToken)
            ?? throw new WalletNotFoundException(walletId);
    }

    /// <summary>Assertions: rejects stale snapshots to prevent lost updates.</summary>
    internal static async Task<WalletBalance> SaveAsync(
        IRewardsWalletRepository repository,
        WalletSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (!await repository.TrySaveAsync(snapshot.Wallet, snapshot.Revision, cancellationToken))
            throw new WalletConflictException("The rewards wallet changed concurrently. Reload its balance and retry.");

        return new(snapshot.Wallet.Id, snapshot.Wallet.Balance.Value);
    }
}
