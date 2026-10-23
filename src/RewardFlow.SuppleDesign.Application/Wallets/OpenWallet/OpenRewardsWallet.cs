using RewardFlow.SuppleDesign.Domain.Entities;
using RewardFlow.SuppleDesign.Domain.Ports;
using RewardFlow.SuppleDesign.Domain.ValueObjects;

namespace RewardFlow.SuppleDesign.Application.Wallets;

/// <summary>Intention-Revealing Interfaces: primary port for opening a rewards wallet.</summary>
/// <remarks>Lets the HTTP adapter invoke this use case without depending on its implementation.</remarks>
/// <seealso href="https://www.oreilly.com/library/view/domain-driven-design-tackling/0321125215/ch10.html">Eric Evans, Chapter 10: Intention-Revealing Interfaces</seealso>
public interface IOpenRewardsWallet
{
    /// <summary>Intention-Revealing Interfaces: opens a named zero-balance wallet with generated identity.</summary>
    Task<WalletOpened> ExecuteAsync(string walletName, CancellationToken cancellationToken = default);
}

/// <summary>Conceptual Contours: coordinates opening and persistence while leaving invariants to the aggregate.</summary>
public sealed class OpenRewardsWallet(IRewardsWalletRepository repository, TimeProvider timeProvider) : IOpenRewardsWallet
{
    /// <inheritdoc />
    public async Task<WalletOpened> ExecuteAsync(string walletName, CancellationToken cancellationToken = default)
    {
        var wallet = new RewardsWallet(Guid.NewGuid(), new WalletName(walletName), timeProvider.GetUtcNow());
        if (!await repository.TryAddAsync(wallet, cancellationToken))
            throw new WalletConflictException("The generated rewards wallet identity is already in use.");

        return new(wallet.Id);
    }
}
