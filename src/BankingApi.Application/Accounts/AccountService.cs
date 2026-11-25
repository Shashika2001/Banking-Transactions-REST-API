using BankingApi.Application.Abstractions;
using BankingApi.Application.Abstractions.Persistence;
using BankingApi.Application.Auditing;
using BankingApi.Application.Common.Exceptions;
using BankingApi.Application.Transactions;
using BankingApi.Domain.Entities;
using FluentValidation;

namespace BankingApi.Application.Accounts;

/// <summary>
/// Default implementation of <see cref="IAccountService"/>.
/// </summary>
public sealed class AccountService : IAccountService
{
    /// <summary>How many generated account numbers to try before giving up.</summary>
    public const int MaxAccountNumberAttempts = 5;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditTrail _auditTrail;
    private readonly TimeProvider _timeProvider;
    private readonly IAccountNumberGenerator _accountNumberGenerator;
    private readonly IValidator<OpenAccountRequest> _openValidator;
    private readonly IValidator<MoneyOperationRequest> _moneyValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountService"/> class.
    /// </summary>
    public AccountService(
        IUnitOfWork unitOfWork,
        IAuditTrail auditTrail,
        TimeProvider timeProvider,
        IAccountNumberGenerator accountNumberGenerator,
        IValidator<OpenAccountRequest> openValidator,
        IValidator<MoneyOperationRequest> moneyValidator)
    {
        _unitOfWork = unitOfWork;
        _auditTrail = auditTrail;
        _timeProvider = timeProvider;
        _accountNumberGenerator = accountNumberGenerator;
        _openValidator = openValidator;
        _moneyValidator = moneyValidator;
    }

    /// <inheritdoc />
    public async Task<AccountResponse> OpenAsync(OpenAccountRequest request, CancellationToken cancellationToken = default)
    {
        await _openValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (!await _unitOfWork.Customers.ExistsAsync(request.CustomerId, cancellationToken))
        {
            throw new NotFoundException(nameof(Customer), request.CustomerId);
        }

        var accountNumber = await GenerateUniqueAccountNumberAsync(cancellationToken);
        var account = Account.Open(
            accountNumber,
            request.CustomerId,
            request.Currency.ToUpperInvariant(),
            _timeProvider.GetUtcNow());

        await _unitOfWork.Accounts.AddAsync(account, cancellationToken);
        await _auditTrail.RecordAsync(
            AuditActions.AccountOpened,
            nameof(Account),
            account.Id,
            new { account.AccountNumber, account.CustomerId, account.Currency },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return account.ToResponse();
    }

    /// <inheritdoc />
    public async Task<AccountResponse> GetByIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var account = await GetRequiredAccountAsync(accountId, cancellationToken);
        return account.ToResponse();
    }

    /// <inheritdoc />
    public async Task<BalanceResponse> GetBalanceAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var account = await GetRequiredAccountAsync(accountId, cancellationToken);
        return account.ToBalanceResponse(_timeProvider.GetUtcNow());
    }

    /// <inheritdoc />
    public async Task<TransactionResponse> DepositAsync(
        Guid accountId,
        MoneyOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        await _moneyValidator.ValidateAndThrowAsync(request, cancellationToken);

        var account = await GetRequiredAccountAsync(accountId, cancellationToken);
        var transaction = account.Deposit(request.Amount, _timeProvider.GetUtcNow(), request.Reference);

        return await SaveMoneyMovementAsync(AuditActions.Deposit, account, transaction, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TransactionResponse> WithdrawAsync(
        Guid accountId,
        MoneyOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        await _moneyValidator.ValidateAndThrowAsync(request, cancellationToken);

        var account = await GetRequiredAccountAsync(accountId, cancellationToken);
        var transaction = account.Withdraw(request.Amount, _timeProvider.GetUtcNow(), request.Reference);

        return await SaveMoneyMovementAsync(AuditActions.Withdrawal, account, transaction, cancellationToken);
    }

    private async Task<TransactionResponse> SaveMoneyMovementAsync(
        string auditAction,
        Account account,
        Transaction transaction,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.Transactions.AddAsync(transaction, cancellationToken);
        await _auditTrail.RecordAsync(
            auditAction,
            nameof(Account),
            account.Id,
            new { TransactionId = transaction.Id, transaction.Amount, account.Currency, transaction.BalanceAfter },
            cancellationToken);

        // Account (with its row version check), transaction and audit entry are saved atomically.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return transaction.ToResponse();
    }

    private async Task<Account> GetRequiredAccountAsync(Guid accountId, CancellationToken cancellationToken) =>
        await _unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), accountId);

    private async Task<string> GenerateUniqueAccountNumberAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAccountNumberAttempts; attempt++)
        {
            var candidate = _accountNumberGenerator.Generate();
            if (!await _unitOfWork.Accounts.AccountNumberExistsAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new ConflictException("Could not generate a unique account number. Please try again.");
    }
}
