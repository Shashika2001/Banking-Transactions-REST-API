using BankingApi.Application.Abstractions.Persistence;
using BankingApi.Application.Auditing;
using BankingApi.Application.Common.Exceptions;
using BankingApi.Application.Common.Security;
using BankingApi.Domain.Entities;
using BankingApi.Domain.Enums;
using BankingApi.Domain.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;

namespace BankingApi.Application.Transfers;

/// <summary>
/// Default implementation of <see cref="ITransferService"/>.
/// </summary>
public sealed class TransferService : ITransferService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditTrail _auditTrail;
    private readonly IAccessPolicy _accessPolicy;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<TransferRequest> _validator;
    private readonly TransferOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransferService"/> class.
    /// </summary>
    public TransferService(
        IUnitOfWork unitOfWork,
        IAuditTrail auditTrail,
        IAccessPolicy accessPolicy,
        TimeProvider timeProvider,
        IValidator<TransferRequest> validator,
        IOptions<TransferOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _unitOfWork = unitOfWork;
        _auditTrail = auditTrail;
        _accessPolicy = accessPolicy;
        _timeProvider = timeProvider;
        _validator = validator;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<TransferResult> TransferAsync(
        TransferRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var key = ValidateIdempotencyKey(idempotencyKey);
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var previous = await FindPreviousResultAsync(key, request, cancellationToken);
        if (previous is not null)
        {
            return new TransferResult(previous, IsReplay: true);
        }

        try
        {
            var transfer = await _unitOfWork.ExecuteInTransactionAsync(
                token => ExecuteTransferAsync(request, key, token),
                cancellationToken);

            return new TransferResult(transfer, IsReplay: false);
        }
        catch (DuplicateKeyException)
        {
            // A concurrent request with the same key committed first; return its result instead.
            var concurrent = await FindPreviousResultAsync(key, request, cancellationToken);
            if (concurrent is not null)
            {
                return new TransferResult(concurrent, IsReplay: true);
            }

            throw;
        }
    }

    private async Task<TransferResponse> ExecuteTransferAsync(TransferRequest request, string key, CancellationToken cancellationToken)
    {
        var source = await GetRequiredAccountAsync(request.SourceAccountId, cancellationToken);

        // Only the owner (or an admin) may send money from an account; any account may receive it.
        _accessPolicy.EnsureCanAccessAccount(source);

        var destination = await GetRequiredAccountAsync(request.DestinationAccountId, cancellationToken);
        var now = _timeProvider.GetUtcNow();

        await EnsureWithinDailyLimitAsync(source.Id, request.Amount, now, cancellationToken);

        var legs = source.TransferTo(destination, request.Amount, now, request.Reference, key);

        await _unitOfWork.Transactions.AddAsync(legs.Outgoing, cancellationToken);
        await _unitOfWork.Transactions.AddAsync(legs.Incoming, cancellationToken);
        await _auditTrail.RecordAsync(
            AuditActions.Transfer,
            nameof(Account),
            source.Id,
            new
            {
                legs.TransferId,
                SourceAccountId = source.Id,
                DestinationAccountId = destination.Id,
                request.Amount,
                source.Currency,
                IdempotencyKey = key,
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(legs.Outgoing, legs.Incoming);
    }

    private async Task EnsureWithinDailyLimitAsync(
        Guid sourceAccountId,
        decimal amount,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var startOfDay = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var transferredToday = await _unitOfWork.Transactions.GetTotalAmountAsync(
            sourceAccountId,
            TransactionType.TransferOut,
            startOfDay,
            startOfDay.AddDays(1),
            cancellationToken);

        if (transferredToday + amount > _options.DailyLimitPerAccount)
        {
            throw new DailyTransferLimitExceededException(
                sourceAccountId,
                _options.DailyLimitPerAccount,
                transferredToday,
                amount);
        }
    }

    // Returns the original transfer when this key was already used for the same request, null when the key is new,
    // and throws when the key was used for a different request.
    private async Task<TransferResponse?> FindPreviousResultAsync(
        string key,
        TransferRequest request,
        CancellationToken cancellationToken)
    {
        var outgoing = await _unitOfWork.Transactions.GetByIdempotencyKeyAsync(key, cancellationToken);
        if (outgoing is null)
        {
            return null;
        }

        if (outgoing.Type != TransactionType.TransferOut || outgoing.TransferId is not { } transferId)
        {
            throw new IdempotencyKeyReusedException("The idempotency key was already used for a different operation.");
        }

        var legs = await _unitOfWork.Transactions.GetByTransferIdAsync(transferId, cancellationToken);
        var incoming = legs.SingleOrDefault(leg => leg.Type == TransactionType.TransferIn)
            ?? throw new InvalidOperationException($"Transfer {transferId} has no incoming leg.");

        var isSameRequest =
            outgoing.AccountId == request.SourceAccountId &&
            incoming.AccountId == request.DestinationAccountId &&
            outgoing.Amount == request.Amount;

        if (!isSameRequest)
        {
            throw new IdempotencyKeyReusedException(
                "The idempotency key was already used for a transfer with different accounts or amount.");
        }

        // A replay must pass the same access check as the original request.
        var source = await GetRequiredAccountAsync(outgoing.AccountId, cancellationToken);
        _accessPolicy.EnsureCanAccessAccount(source);

        return ToResponse(outgoing, incoming);
    }

    private async Task<Account> GetRequiredAccountAsync(Guid accountId, CancellationToken cancellationToken) =>
        await _unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), accountId);

    private static string ValidateIdempotencyKey(string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ValidationException(
            [
                new ValidationFailure(IdempotencyKey.HeaderName, $"The {IdempotencyKey.HeaderName} header is required."),
            ]);
        }

        var key = idempotencyKey.Trim();
        if (key.Length > Transaction.IdempotencyKeyMaxLength)
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    IdempotencyKey.HeaderName,
                    $"The {IdempotencyKey.HeaderName} header must be at most {Transaction.IdempotencyKeyMaxLength} characters."),
            ]);
        }

        return key;
    }

    private static TransferResponse ToResponse(Transaction outgoing, Transaction incoming) =>
        new(
            outgoing.TransferId!.Value,
            outgoing.AccountId,
            incoming.AccountId,
            outgoing.Amount,
            outgoing.BalanceAfter,
            outgoing.Reference,
            outgoing.CreatedAt);
}
