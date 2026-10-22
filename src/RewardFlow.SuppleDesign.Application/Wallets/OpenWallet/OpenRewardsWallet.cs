using RewardFlow.SuppleDesign.Domain.Entities;
using RewardFlow.SuppleDesign.Domain.Ports;

namespace RewardFlow.SuppleDesign.Application.Wallets;

/// <summary>Intention-Revealing Interfaces: primary port for opening a rewards wallet.</summary>
/// <remarks>Lets the HTTP adapter invoke this use case without depending on its implementation.</remarks>
/// <seealso href="https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html">Eric Evans, Chapter 10: Intention-Revealing Interfaces</seealso>
public interface IOpenRewardsWallet
{
    /// <summary>Intention-Revealing Interfaces: opens a zero-balance wallet.</summary>
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
