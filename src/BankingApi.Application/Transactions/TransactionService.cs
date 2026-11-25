using BankingApi.Application.Abstractions.Persistence;
using BankingApi.Application.Common.Exceptions;
using BankingApi.Application.Common.Models;
using BankingApi.Application.Common.Security;
using BankingApi.Domain.Entities;
using FluentValidation;

namespace BankingApi.Application.Transactions;

/// <summary>
/// Default implementation of <see cref="ITransactionService"/>.
/// </summary>
public sealed class TransactionService : ITransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccessPolicy _accessPolicy;
    private readonly IValidator<TransactionHistoryQuery> _queryValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionService"/> class.
    /// </summary>
    public TransactionService(
        IUnitOfWork unitOfWork,
        IAccessPolicy accessPolicy,
        IValidator<TransactionHistoryQuery> queryValidator)
    {
        _unitOfWork = unitOfWork;
        _accessPolicy = accessPolicy;
        _queryValidator = queryValidator;
    }

    /// <inheritdoc />
    public async Task<PagedResult<TransactionResponse>> GetHistoryAsync(
        Guid accountId,
        TransactionHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        await _queryValidator.ValidateAndThrowAsync(query, cancellationToken);

        var account = await _unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), accountId);
        _accessPolicy.EnsureCanAccessAccount(account);

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
