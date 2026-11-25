using BankingApi.Application.Abstractions.Persistence;
using BankingApi.Application.Common.Exceptions;
using BankingApi.Application.Common.Models;
using BankingApi.Domain.Entities;
using FluentValidation;

namespace BankingApi.Application.Transactions;

/// <summary>
/// Default implementation of <see cref="ITransactionService"/>.
/// </summary>
public sealed class TransactionService : ITransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<TransactionHistoryQuery> _queryValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionService"/> class.
    /// </summary>
    public TransactionService(IUnitOfWork unitOfWork, IValidator<TransactionHistoryQuery> queryValidator)
    {
        _unitOfWork = unitOfWork;
        _queryValidator = queryValidator;
    }

    /// <inheritdoc />
    public async Task<PagedResult<TransactionResponse>> GetHistoryAsync(
        Guid accountId,
        TransactionHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        await _queryValidator.ValidateAndThrowAsync(query, cancellationToken);

        if (!await _unitOfWork.Accounts.ExistsAsync(accountId, cancellationToken))
        {
            throw new NotFoundException(nameof(Account), accountId);
        }

        var page = await _unitOfWork.Transactions.GetHistoryAsync(
            accountId,
            query.From,
            query.To,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        return page.Map(transaction => transaction.ToResponse());
    }
}
