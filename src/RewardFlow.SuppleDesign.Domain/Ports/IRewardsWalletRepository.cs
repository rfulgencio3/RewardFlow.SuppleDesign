using RewardFlow.SuppleDesign.Domain.Entities;

namespace RewardFlow.SuppleDesign.Domain.Ports;

/// <summary>Conceptual Contours: a detached aggregate and its persistence revision, outside wallet business behavior.</summary>
/// <remarks>The revision exists because concurrent commands need an explicit lost-update guard.</remarks>
public sealed record WalletSnapshot(RewardsWallet Wallet, long Revision);

/// <summary>Intention-Revealing Interfaces: secondary port for loading and saving rewards wallets.</summary>
/// <remarks>The port is justified by a real adapter boundary: the in-memory store can be replaced without changing use cases.</remarks>
/// <seealso href="https://www.domainlanguage.com/ddd/reference/">Eric Evans' DDD Reference: Repositories</seealso>
public interface IRewardsWalletRepository
{
    /// <summary>Repository query: loads an isolated wallet snapshot, or null when absent.</summary>
    Task<WalletSnapshot?> FindAsync(Guid walletId, CancellationToken cancellationToken = default);
    /// <summary>Intention-Revealing Interfaces: adds a wallet only if its identity is unused.</summary>
    Task<bool> TryAddAsync(RewardsWallet wallet, CancellationToken cancellationToken = default);
    /// <summary>Assertions: saves only if the persisted revision still matches the loaded revision.</summary>
    Task<bool> TrySaveAsync(RewardsWallet wallet, long expectedRevision, CancellationToken cancellationToken = default);
}
