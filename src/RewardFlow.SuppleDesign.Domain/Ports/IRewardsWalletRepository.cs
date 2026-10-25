using RewardFlow.SuppleDesign.Domain.Entities;

namespace RewardFlow.SuppleDesign.Domain.Ports;

/// <summary>Conceptual Contours: a detached aggregate and its persistence revision, outside wallet business behavior.</summary>
/// <remarks>The revision exists because concurrent commands need an explicit lost-update guard.</remarks>
public sealed record WalletSnapshot(RewardsWallet Wallet, long Revision);

/// <summary>Outcome of atomically saving a wallet balance and its transaction record.</summary>
public enum MovementSaveResult
{
    /// <summary>Wallet and transaction were committed together.</summary>
    Saved,
    /// <summary>The loaded wallet revision is stale.</summary>
    RevisionConflict,
    /// <summary>The operation identity was previously committed.</summary>
    DuplicateOperation
}

/// <summary>Intention-Revealing Interfaces: secondary port for loading and saving rewards wallets.</summary>
/// <remarks>The port is justified by a real adapter boundary: PostgreSQL or a test double can serve it without changing use cases.</remarks>
/// <seealso href="https://www.domainlanguage.com/ddd/reference/">Eric Evans' DDD Reference: Repositories</seealso>
public interface IRewardsWalletRepository
{
    /// <summary>Reads the global namespace shared by credit, debit and transfer operations.</summary>
    Task<RewardOperationKind?> FindOperationKindAsync(Guid operationId, CancellationToken cancellationToken = default);
    /// <summary>Loads the immutable original transfer result, if present.</summary>
    Task<RewardTransfer?> FindTransferAsync(Guid operationId, CancellationToken cancellationToken = default);
    /// <summary>Atomically saves both balances, revisions and paired ledger entries.</summary>
    Task<MovementSaveResult> TrySaveTransferAsync(RewardTransfer transfer, long sourceRevision,
        long destinationRevision, CancellationToken cancellationToken = default);
    /// <summary>Repository query: loads an isolated wallet snapshot, or null when absent.</summary>
    Task<WalletSnapshot?> FindAsync(Guid walletId, CancellationToken cancellationToken = default);
    /// <summary>Intention-Revealing Interfaces: adds a wallet only if its identity is unused.</summary>
    Task<bool> TryAddAsync(RewardsWallet wallet, CancellationToken cancellationToken = default);
    /// <summary>Assertions: saves only if the persisted revision still matches the loaded revision.</summary>
    Task<bool> TrySaveAsync(RewardsWallet wallet, long expectedRevision, CancellationToken cancellationToken = default);
    /// <summary>Atomically saves a balance change and its immutable transaction record.</summary>
    Task<MovementSaveResult> TrySaveMovementAsync(RewardsWallet wallet, RewardTransaction transaction,
        long expectedRevision, CancellationToken cancellationToken = default);
    /// <summary>Finds an ordinary credit/debit result; transfer entries are read through their wallet ledger or FindTransferAsync.</summary>
    Task<RewardTransaction?> FindTransactionAsync(Guid operationId, CancellationToken cancellationToken = default);
    /// <summary>Returns a page of wallet transactions in reverse chronological order.</summary>
    Task<IReadOnlyList<RewardTransaction>> ListTransactionsAsync(Guid walletId, int skip, int take,
        CancellationToken cancellationToken = default);
}
