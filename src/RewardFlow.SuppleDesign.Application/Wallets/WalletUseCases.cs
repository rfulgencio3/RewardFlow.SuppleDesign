using RewardFlow.SuppleDesign.Domain;
using RewardFlow.SuppleDesign.Domain.Entities;
using RewardFlow.SuppleDesign.Domain.Ports;
using RewardFlow.SuppleDesign.Domain.ValueObjects;

namespace RewardFlow.SuppleDesign.Application.Wallets;

/// <summary>Intention-Revealing Interfaces: application result independent of HTTP contracts.</summary>
public sealed record WalletBalance(Guid WalletId, long Points);

/// <summary>Intention-Revealing Interfaces: an application failure understood by primary adapters.</summary>
public sealed class WalletNotFoundException(Guid walletId) : Exception($"Rewards wallet '{walletId}' was not found.");

/// <summary>Assertions: signals a duplicate identity or a concurrent write that the caller can retry.</summary>
public sealed class WalletConflictException(string message) : Exception(message);

/// <summary>Intention-Revealing Interfaces: primary port for opening a rewards wallet.</summary>
/// <seealso href="https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html">Eric Evans, Chapter 10: Intention-Revealing Interfaces</seealso>
public interface IOpenRewardsWallet
{
    /// <summary>Intention-Revealing Interfaces: opens a zero-balance wallet.</summary>
    Task<WalletBalance> ExecuteAsync(Guid walletId, CancellationToken cancellationToken = default);
}

/// <summary>Intention-Revealing Interfaces: primary port for earned-point credits.</summary>
public interface ICreditRewardPoints
{
    /// <summary>Intention-Revealing Interfaces: credits points and returns the saved balance.</summary>
    Task<WalletBalance> ExecuteAsync(Guid walletId, long points, CancellationToken cancellationToken = default);
}

/// <summary>Intention-Revealing Interfaces: primary port for reward redemption.</summary>
public interface IDebitRewardPoints
{
    /// <summary>Intention-Revealing Interfaces: debits points and returns the saved balance.</summary>
    Task<WalletBalance> ExecuteAsync(Guid walletId, long points, CancellationToken cancellationToken = default);
}

/// <summary>Side-Effect-Free Functions: primary query port for a wallet's balance.</summary>
public interface IGetWalletBalance
{
    /// <summary>Side-Effect-Free Functions: reads without creating or changing a wallet.</summary>
    Task<WalletBalance> ExecuteAsync(Guid walletId, CancellationToken cancellationToken = default);
}

/// <summary>Conceptual Contours: coordinates opening and persistence while leaving invariants to the aggregate.</summary>
public sealed class OpenRewardsWallet(IRewardsWalletRepository repository) : IOpenRewardsWallet
{
    /// <inheritdoc />
    public async Task<WalletBalance> ExecuteAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var wallet = new RewardsWallet(walletId);
        if (!await repository.TryAddAsync(wallet, cancellationToken))
            throw new WalletConflictException("A rewards wallet with this ID already exists.");
        return new(wallet.Id, wallet.Balance.Value);
    }
}

/// <summary>Conceptual Contours: coordinates a credit through the aggregate and secondary port.</summary>
public sealed class CreditRewardPoints(IRewardsWalletRepository repository) : ICreditRewardPoints
{
    /// <inheritdoc />
    public async Task<WalletBalance> ExecuteAsync(Guid walletId, long points, CancellationToken cancellationToken = default)
    {
        var earnedPoints = new RewardPoints(points);
        var snapshot = await WalletPersistence.LoadAsync(repository, walletId, cancellationToken);
        snapshot.Wallet.Credit(earnedPoints);
        return await WalletPersistence.SaveAsync(repository, snapshot, cancellationToken);
    }
}

/// <summary>Conceptual Contours: coordinates redemption without duplicating wallet rules.</summary>
public sealed class DebitRewardPoints(IRewardsWalletRepository repository) : IDebitRewardPoints
{
    /// <inheritdoc />
    public async Task<WalletBalance> ExecuteAsync(Guid walletId, long points, CancellationToken cancellationToken = default)
    {
        var redeemedPoints = new RewardPoints(points);
        var snapshot = await WalletPersistence.LoadAsync(repository, walletId, cancellationToken);
        snapshot.Wallet.Debit(redeemedPoints);
        return await WalletPersistence.SaveAsync(repository, snapshot, cancellationToken);
    }
}

/// <summary>Side-Effect-Free Functions: coordinates reading a wallet without persistence writes.</summary>
public sealed class GetWalletBalance(IRewardsWalletRepository repository) : IGetWalletBalance
{
    /// <inheritdoc />
    public async Task<WalletBalance> ExecuteAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var snapshot = await WalletPersistence.LoadAsync(repository, walletId, cancellationToken);
        return new(snapshot.Wallet.Id, snapshot.Wallet.Balance.Value);
    }
}

/// <summary>Conceptual Contours: shared application persistence coordination.</summary>
internal static class WalletPersistence
{
    /// <summary>Assertions: a command or query must address an existing, identified wallet.</summary>
    internal static async Task<WalletSnapshot> LoadAsync(IRewardsWalletRepository repository, Guid walletId, CancellationToken token)
    {
        DomainAssertions.WalletIdentity(walletId);
        return await repository.FindAsync(walletId, token) ?? throw new WalletNotFoundException(walletId);
    }

    /// <summary>Assertions: rejects stale snapshots to prevent lost updates.</summary>
    internal static async Task<WalletBalance> SaveAsync(IRewardsWalletRepository repository, WalletSnapshot snapshot, CancellationToken token)
    {
        if (!await repository.TrySaveAsync(snapshot.Wallet, snapshot.Revision, token))
            throw new WalletConflictException("The rewards wallet changed concurrently. Reload its balance and retry.");
        return new(snapshot.Wallet.Id, snapshot.Wallet.Balance.Value);
    }
}
