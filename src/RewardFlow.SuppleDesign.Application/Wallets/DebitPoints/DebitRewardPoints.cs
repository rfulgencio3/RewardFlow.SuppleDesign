using RewardFlow.SuppleDesign.Domain.Ports;
using RewardFlow.SuppleDesign.Domain.ValueObjects;

namespace RewardFlow.SuppleDesign.Application.Wallets;

/// <summary>Intention-Revealing Interfaces: primary port for reward redemption.</summary>
/// <remarks>Lets primary adapters depend on the redemption capability rather than its orchestration details.</remarks>
public interface IDebitRewardPoints
{
    /// <summary>Intention-Revealing Interfaces: debits points and returns the saved balance.</summary>
    Task<WalletBalance> ExecuteAsync(Guid walletId, long points, CancellationToken cancellationToken = default);
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
