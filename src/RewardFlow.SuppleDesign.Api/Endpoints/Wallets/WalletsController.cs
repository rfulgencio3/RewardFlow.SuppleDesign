using Microsoft.AspNetCore.Mvc;
using RewardFlow.SuppleDesign.Application.Wallets;
using RewardFlow.SuppleDesign.Api.DTOs.Wallets;

namespace RewardFlow.SuppleDesign.Api.Endpoints.Wallets;

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
    /// <summary>Opens a rewards wallet with a zero balance.</summary>
    [HttpPost("{id:guid}")]
    public async Task<ActionResult<WalletBalanceResponse>> Open(Guid id, CancellationToken cancellationToken)
    {
        var result = await openWallet.ExecuteAsync(id, cancellationToken);
        return CreatedAtAction(nameof(Balance), new { id }, new WalletBalanceResponse(result.WalletId, result.Points));
    }

    /// <summary>Credits earned points to a rewards wallet.</summary>
    [HttpPost("{id:guid}/credit")]
    public async Task<ActionResult<WalletBalanceResponse>> Credit(Guid id, PointsRequest request, CancellationToken cancellationToken)
    {
        var result = await creditPoints.ExecuteAsync(id, request.Points!.Value, cancellationToken);
        return Ok(new WalletBalanceResponse(result.WalletId, result.Points));
    }

    /// <summary>Debits redeemed points from a rewards wallet.</summary>
    [HttpPost("{id:guid}/debit")]
    public async Task<ActionResult<WalletBalanceResponse>> Debit(Guid id, PointsRequest request, CancellationToken cancellationToken)
    {
        var result = await debitPoints.ExecuteAsync(id, request.Points!.Value, cancellationToken);
        return Ok(new WalletBalanceResponse(result.WalletId, result.Points));
    }

    /// <summary>Gets the current rewards wallet balance.</summary>
    [HttpGet("{id:guid}/balance")]
    public async Task<ActionResult<WalletBalanceResponse>> Balance(Guid id, CancellationToken cancellationToken)
    {
        var result = await getBalance.ExecuteAsync(id, cancellationToken);
        return Ok(new WalletBalanceResponse(result.WalletId, result.Points));
    }

    /// <summary>Checks whether the wallet can afford a points amount.</summary>
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
