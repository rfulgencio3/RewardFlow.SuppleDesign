using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RewardFlow.SuppleDesign.Application.Wallets;
using RewardFlow.SuppleDesign.Domain.Exceptions;

namespace RewardFlow.SuppleDesign.Api.Errors;

/// <summary>Conceptual Contours: translates domain/application failures into HTTP Problem Details only at the boundary.</summary>
public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    /// <summary>Intention-Revealing Interfaces: maps known failures without exposing unexpected internal details.</summary>
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, code) = exception switch
        {
            DomainException domain => (StatusCodes.Status422UnprocessableEntity, "Reward rule violated", domain.Code),
            WalletNotFoundException => (StatusCodes.Status404NotFound, "Rewards wallet not found", "wallet_not_found"),
            WalletConflictException => (StatusCodes.Status409Conflict, "Rewards wallet conflict", "wallet_conflict"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", "internal_error")
        };
        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status == 500 ? "The request could not be completed." : exception.Message,
                Extensions = { ["code"] = code }
            }
        });
    }
}
