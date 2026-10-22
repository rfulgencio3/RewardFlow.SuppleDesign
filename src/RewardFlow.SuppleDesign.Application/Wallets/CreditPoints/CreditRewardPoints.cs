using RewardFlow.SuppleDesign.Domain.Ports;
using RewardFlow.SuppleDesign.Domain.ValueObjects;

namespace RewardFlow.SuppleDesign.Application.Wallets;

/// <summary>Intention-Revealing Interfaces: primary port for earned-point credits.</summary>
public interface ICreditRewardPoints
{
    /// <summary>Intention-Revealing Interfaces: credits points and returns the saved balance.</summary>
    Task<WalletBalance> ExecuteAsync(Guid walletId, long points, CancellationToken cancellationToken = default);
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
