using BankingApi.Application.Common.Exceptions;
using BankingApi.Application.Transactions;
using BankingApi.Domain.Exceptions;
using FluentValidation;

namespace BankingApi.Application.Accounts;

/// <summary>
/// Account use cases.
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Opens a new account for an existing customer.
    /// </summary>
    /// <param name="request">The account details.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The opened account.</returns>
    /// <exception cref="ValidationException">Thrown when the request is invalid.</exception>
    /// <exception cref="ForbiddenAccessException">Thrown when a customer opens an account for someone else.</exception>
    /// <exception cref="NotFoundException">Thrown when the customer does not exist.</exception>
    /// <exception cref="ConflictException">Thrown when no unique account number could be generated.</exception>
    Task<AccountResponse> OpenAsync(OpenAccountRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The account.</returns>
    /// <exception cref="NotFoundException">Thrown when the account does not exist.</exception>
    /// <exception cref="ForbiddenAccessException">Thrown when the caller is neither an admin nor the account owner.</exception>
    Task<AccountResponse> GetByIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an account's current balance.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The balance.</returns>
    /// <exception cref="NotFoundException">Thrown when the account does not exist.</exception>
    /// <exception cref="ForbiddenAccessException">Thrown when the caller is neither an admin nor the account owner.</exception>
    Task<BalanceResponse> GetBalanceAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deposits money into an account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="request">The amount and reference.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The recorded transaction.</returns>
    /// <exception cref="ValidationException">Thrown when the request is invalid.</exception>
    /// <exception cref="NotFoundException">Thrown when the account does not exist.</exception>
    /// <exception cref="ForbiddenAccessException">Thrown when the caller is neither an admin nor the account owner.</exception>
    /// <exception cref="DomainException">Thrown when a business rule is violated (for example, the account is frozen).</exception>
    /// <exception cref="ConcurrencyConflictException">Thrown when the account was changed by another request.</exception>
    Task<TransactionResponse> DepositAsync(Guid accountId, MoneyOperationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Withdraws money from an account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="request">The amount and reference.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The recorded transaction.</returns>
    /// <exception cref="ValidationException">Thrown when the request is invalid.</exception>
    /// <exception cref="NotFoundException">Thrown when the account does not exist.</exception>
    /// <exception cref="ForbiddenAccessException">Thrown when the caller is neither an admin nor the account owner.</exception>
    /// <exception cref="DomainException">Thrown when a business rule is violated (for example, insufficient funds).</exception>
    /// <exception cref="ConcurrencyConflictException">Thrown when the account was changed by another request.</exception>
    Task<TransactionResponse> WithdrawAsync(Guid accountId, MoneyOperationRequest request, CancellationToken cancellationToken = default);
}
