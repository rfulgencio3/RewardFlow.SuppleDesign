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
    ICloseRewardsWallet closeWallet,
    IListRewardTransactions listTransactions) : ControllerBase
{
    /// <summary>Opens a named rewards wallet with generated identity, active status and zero balance.</summary>
    [HttpPost]
    [ProducesResponseType<OpenWalletResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
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
    [ProducesResponseType<WalletDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
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
    [ProducesResponseType<RewardMovementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<RewardMovementResponse>> Credit(Guid id, RewardMovementRequest request, CancellationToken cancellationToken)
    {
        var result = await creditPoints.ExecuteAsync(id, request.OperationId!.Value, request.Points!.Value,
            request.Reason!, cancellationToken);
        return Ok(ToResponse(result));
    }

    /// <summary>Debits redeemed points from a rewards wallet.</summary>
    [HttpPost("{id:guid}/debit")]
    [ProducesResponseType<RewardMovementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<RewardMovementResponse>> Debit(Guid id, RewardMovementRequest request, CancellationToken cancellationToken)
    {
        var result = await debitPoints.ExecuteAsync(id, request.OperationId!.Value, request.Points!.Value,
            request.Reason!, cancellationToken);
        return Ok(ToResponse(result));
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

    /// <summary>Lists immutable reward movements in reverse chronological order.</summary>
    [HttpGet("{id:guid}/transactions")]
    [ProducesResponseType<RewardTransactionPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RewardTransactionPageResponse>> Transactions(Guid id,
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 1_000_000)] int page = 1,
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 100)] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await listTransactions.ExecuteAsync(id, page, pageSize, cancellationToken);
        return Ok(new RewardTransactionPageResponse(result.Items.Select(ToResponse).ToArray(), result.Page,
            result.PageSize, result.HasNextPage));
    }

    private static RewardMovementResponse ToResponse(RewardMovement movement) => new(movement.OperationId,
        movement.Type, movement.Points, movement.Reason, movement.OccurredAtUtc, movement.BalanceAfter);
}
