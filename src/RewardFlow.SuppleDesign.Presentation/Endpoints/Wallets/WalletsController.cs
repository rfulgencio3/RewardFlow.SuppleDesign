using Microsoft.AspNetCore.Mvc;
using RewardFlow.SuppleDesign.Application.Wallets;
using RewardFlow.SuppleDesign.Presentation.DTOs.Wallets;

namespace RewardFlow.SuppleDesign.Presentation.Endpoints.Wallets;

/// <summary>Intention-Revealing Interfaces: HTTP primary adapter delegates business execution to application ports.</summary>
[ApiController]
[Route("api/wallets")]
public sealed class WalletsController(
    IOpenRewardsWallet openWallet,
    ICreditRewardPoints creditPoints,
    IDebitRewardPoints debitPoints,
    IGetWalletBalance getBalance,
    ICheckWalletAffordability checkAffordability) : ControllerBase
{
    /// <summary>Intention-Revealing Interfaces: explicitly opens a wallet, returning its balance location.</summary>
    [HttpPost("{id:guid}")]
    public async Task<ActionResult<WalletBalanceResponse>> Open(Guid id, CancellationToken cancellationToken)
    {
        var result = await openWallet.ExecuteAsync(id, cancellationToken);
        return CreatedAtAction(nameof(Balance), new { id }, new WalletBalanceResponse(result.WalletId, result.Points));
    }

    /// <summary>Intention-Revealing Interfaces: credits earned reward points.</summary>
    [HttpPost("{id:guid}/credit")]
    public async Task<ActionResult<WalletBalanceResponse>> Credit(Guid id, PointsRequest request, CancellationToken cancellationToken)
    {
        var result = await creditPoints.ExecuteAsync(id, request.Points!.Value, cancellationToken);
        return Ok(new WalletBalanceResponse(result.WalletId, result.Points));
    }

    /// <summary>Intention-Revealing Interfaces: debits redeemed reward points.</summary>
    [HttpPost("{id:guid}/debit")]
    public async Task<ActionResult<WalletBalanceResponse>> Debit(Guid id, PointsRequest request, CancellationToken cancellationToken)
    {
        var result = await debitPoints.ExecuteAsync(id, request.Points!.Value, cancellationToken);
        return Ok(new WalletBalanceResponse(result.WalletId, result.Points));
    }

    /// <summary>Query: reads balance without creating or mutating a wallet.</summary>
    [HttpGet("{id:guid}/balance")]
    public async Task<ActionResult<WalletBalanceResponse>> Balance(Guid id, CancellationToken cancellationToken)
    {
        var result = await getBalance.ExecuteAsync(id, cancellationToken);
        return Ok(new WalletBalanceResponse(result.WalletId, result.Points));
    }

    /// <summary>Exposes the wallet's side-effect-free affordability function without mutating its balance.</summary>
    [HttpGet("{id:guid}/can-afford")]
    public async Task<ActionResult<WalletAffordabilityResponse>> CanAfford(
        Guid id,
        [FromQuery,
         Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired,
         System.ComponentModel.DataAnnotations.Range(typeof(long), "0", "9223372036854775807")]
        long points,
        CancellationToken cancellationToken)
    {
        var result = await checkAffordability.ExecuteAsync(id, points, cancellationToken);
        return Ok(new WalletAffordabilityResponse(
            result.WalletId,
            result.Balance,
            result.RequiredPoints,
            result.CanAfford));
    }
}
