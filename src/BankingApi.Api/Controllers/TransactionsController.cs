using BankingApi.Application.Common.Models;
using BankingApi.Application.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.Api.Controllers;

/// <summary>
/// Reads an account's transaction history. Customers may only read their own accounts.
/// </summary>
[ApiController]
[Route("api/accounts/{accountId:guid}/transactions")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionsController"/> class.
    /// </summary>
    /// <param name="transactionService">The transaction service.</param>
    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    /// <summary>
    /// Gets an account's transactions, newest first.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="query">
    /// Paging (<c>pageNumber</c>, <c>pageSize</c> up to 100) and an optional time range: <c>from</c> (inclusive)
    /// and <c>to</c> (exclusive), as ISO 8601 timestamps.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>One page of transactions.</returns>
    [HttpGet]
    [ProducesResponseType<PagedResult<TransactionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<TransactionResponse>>> GetHistory(
        Guid accountId,
        [FromQuery] TransactionHistoryQuery query,
        CancellationToken cancellationToken) =>
        await _transactionService.GetHistoryAsync(accountId, query, cancellationToken);
}
