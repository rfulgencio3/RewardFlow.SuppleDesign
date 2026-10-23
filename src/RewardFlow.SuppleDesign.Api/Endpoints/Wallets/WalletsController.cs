using Microsoft.AspNetCore.Mvc;
using RewardFlow.SuppleDesign.Application.Wallets;
using RewardFlow.SuppleDesign.Api.DTOs.Wallets;

namespace RewardFlow.SuppleDesign.Api.Endpoints.Wallets;

/// <summary>Intention-Revealing Interfaces: HTTP primary adapter delegates business execution to application ports.</summary>
[ApiController]
[Route("api/wallets")]
public sealed class WalletsController(
    IOpenRewardsWallet openWallet,
    IGetRewardsWallet getWallet,
    ICreditRewardPoints creditPoints,
    IDebitRewardPoints debitPoints,
    IGetWalletBalance getBalance,
    ICheckWalletAffordability checkAffordability,
    ISuspendRewardsWallet suspendWallet,
    IReactivateRewardsWallet reactivateWallet,
    ICloseRewardsWallet closeWallet) : ControllerBase
{
    /// <summary>Opens a named rewards wallet with generated identity, active status and zero balance.</summary>
    [HttpPost]
    public async Task<ActionResult<OpenWalletResponse>> Open(
        OpenWalletRequest request,
        CancellationToken cancellationToken)
    {
        var result = await openWallet.ExecuteAsync(request.Name!, cancellationToken);
        return CreatedAtAction(
            nameof(Get),
            new { id = result.WalletId },
            new OpenWalletResponse(result.WalletId));
    }

    /// <summary>Gets the wallet's name, lifecycle state, creation time and balance.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WalletDetailsResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await getWallet.ExecuteAsync(id, cancellationToken);
        return Ok(new WalletDetailsResponse(
            result.WalletId,
            result.Name,
            result.Status,
            result.CreatedAtUtc,
            result.Points));
    }

    /// <summary>Credits earned points to a rewards wallet.</summary>
    [HttpPost("{id:guid}/credit")]
    public async Task<ActionResult<WalletBalanceResponse>> Credit(Guid id, PointsRequest request, CancellationToken cancellationToken)
    {
        var result = await creditPoints.ExecuteAsync(id, request.Points!.Value, cancellationToken);
        return Ok(new WalletBalanceResponse(result.Points));
    }

    /// <summary>Debits redeemed points from a rewards wallet.</summary>
    [HttpPost("{id:guid}/debit")]
    public async Task<ActionResult<WalletBalanceResponse>> Debit(Guid id, PointsRequest request, CancellationToken cancellationToken)
    {
        var result = await debitPoints.ExecuteAsync(id, request.Points!.Value, cancellationToken);
        return Ok(new WalletBalanceResponse(result.Points));
    }

    /// <summary>Gets the current rewards wallet balance.</summary>
    [HttpGet("{id:guid}/balance")]
    public async Task<ActionResult<WalletBalanceResponse>> Balance(Guid id, CancellationToken cancellationToken)
    {
        var result = await getBalance.ExecuteAsync(id, cancellationToken);
        return Ok(new WalletBalanceResponse(result.Points));
    }

    /// <summary>Checks whether the wallet can afford a points amount.</summary>
    [HttpGet("{id:guid}/can-afford")]
    public async Task<ActionResult<WalletAffordabilityResponse>> CanAfford(
        Guid id,
        [FromQuery,
         Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired,
         System.ComponentModel.DataAnnotations.Range(typeof(long), "0", "9223372036854775807")]
        long requiredPoints,
        CancellationToken cancellationToken)
    {
        var result = await checkAffordability.ExecuteAsync(id, requiredPoints, cancellationToken);
        return Ok(new WalletAffordabilityResponse(
            result.AvailablePoints,
            result.RequiredPoints,
            result.CanAfford));
    }

    /// <summary>Temporarily suspends redemptions while still allowing earned-point credits.</summary>
    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken cancellationToken)
    {
        await suspendWallet.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Returns a suspended wallet to active use.</summary>
    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken cancellationToken)
    {
        await reactivateWallet.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Permanently closes a wallet and rejects further point movements.</summary>
    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        await closeWallet.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }
}
