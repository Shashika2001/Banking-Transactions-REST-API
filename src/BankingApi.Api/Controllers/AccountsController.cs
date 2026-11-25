using BankingApi.Application.Accounts;
using BankingApi.Application.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.Api.Controllers;

/// <summary>
/// Opens accounts, reads them, and deposits or withdraws money. Customers may only use their own accounts.
/// </summary>
[ApiController]
[Route("api/accounts")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountsController"/> class.
    /// </summary>
    /// <param name="accountService">The account service.</param>
    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    /// <summary>
    /// Opens an account for an existing customer.
    /// </summary>
    /// <param name="request">The customer and currency.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The opened account.</returns>
    [HttpPost]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountResponse>> Open(OpenAccountRequest request, CancellationToken cancellationToken)
    {
        var account = await _accountService.OpenAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { accountId = account.Id }, account);
    }

    /// <summary>
    /// Gets an account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The account.</returns>
    [HttpGet("{accountId:guid}")]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountResponse>> GetById(Guid accountId, CancellationToken cancellationToken) =>
        await _accountService.GetByIdAsync(accountId, cancellationToken);

    /// <summary>
    /// Gets an account's current balance.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The balance.</returns>
    [HttpGet("{accountId:guid}/balance")]
    [ProducesResponseType<BalanceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BalanceResponse>> GetBalance(Guid accountId, CancellationToken cancellationToken) =>
        await _accountService.GetBalanceAsync(accountId, cancellationToken);

    /// <summary>
    /// Deposits money into an account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="request">The amount and optional reference.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The recorded transaction.</returns>
    [HttpPost("{accountId:guid}/deposits")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TransactionResponse>> Deposit(
        Guid accountId,
        MoneyOperationRequest request,
        CancellationToken cancellationToken) =>
        await _accountService.DepositAsync(accountId, request, cancellationToken);

    /// <summary>
    /// Withdraws money from an account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="request">The amount and optional reference.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The recorded transaction.</returns>
    [HttpPost("{accountId:guid}/withdrawals")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TransactionResponse>> Withdraw(
        Guid accountId,
        MoneyOperationRequest request,
        CancellationToken cancellationToken) =>
        await _accountService.WithdrawAsync(accountId, request, cancellationToken);
}
