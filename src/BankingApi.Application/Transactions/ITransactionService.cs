using BankingApi.Application.Common.Exceptions;
using BankingApi.Application.Common.Models;
using FluentValidation;

namespace BankingApi.Application.Transactions;

/// <summary>
/// Transaction history use cases.
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// Gets one page of an account's transactions, newest first.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="query">Paging and optional date range.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The requested page.</returns>
    /// <exception cref="ValidationException">Thrown when the query is invalid.</exception>
    /// <exception cref="NotFoundException">Thrown when the account does not exist.</exception>
    /// <exception cref="ForbiddenAccessException">Thrown when the caller is neither an admin nor the account owner.</exception>
    Task<PagedResult<TransactionResponse>> GetHistoryAsync(
        Guid accountId,
        TransactionHistoryQuery query,
        CancellationToken cancellationToken = default);
}
