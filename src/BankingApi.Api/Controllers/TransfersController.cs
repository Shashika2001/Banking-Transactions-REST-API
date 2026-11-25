using BankingApi.Application.Transfers;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.Api.Controllers;

/// <summary>
/// Transfers money between accounts.
/// </summary>
[ApiController]
[Route("api/transfers")]
[Produces("application/json")]
public sealed class TransfersController : ControllerBase
{
    /// <summary>Response header set to <c>true</c> when an earlier result is returned for a repeated idempotency key.</summary>
    public const string ReplayedHeaderName = "Idempotent-Replayed";

    private readonly ITransferService _transferService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransfersController"/> class.
    /// </summary>
    /// <param name="transferService">The transfer service.</param>
    public TransfersController(ITransferService transferService)
    {
        _transferService = transferService;
    }

    /// <summary>
    /// Transfers money between two accounts of the same currency.
    /// </summary>
    /// <remarks>
    /// Requires an <c>Idempotency-Key</c> header. Repeating a request with the same key returns the original
    /// result (with an <c>Idempotent-Replayed: true</c> header) instead of moving money again. Reusing a key for a
    /// different request returns 409.
    /// </remarks>
    /// <param name="request">The transfer details.</param>
    /// <param name="idempotencyKey">A unique client-generated key (for example, a UUID), up to 64 characters.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The transfer.</returns>
    [HttpPost]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TransferResponse>> Create(
        TransferRequest request,
        [FromHeader(Name = IdempotencyKey.HeaderName)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _transferService.TransferAsync(request, idempotencyKey, cancellationToken);

        Response.Headers[ReplayedHeaderName] = result.IsReplay ? "true" : "false";
        return StatusCode(StatusCodes.Status201Created, result.Transfer);
    }
}
