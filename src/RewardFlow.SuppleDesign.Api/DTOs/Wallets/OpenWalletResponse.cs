namespace RewardFlow.SuppleDesign.Api.DTOs.Wallets;

/// <summary>Minimal creation response; HTTP 201 and Location carry the remaining creation semantics.</summary>
public sealed record OpenWalletResponse(Guid WalletId);
