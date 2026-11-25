using BankingApi.Application.Common.Exceptions;
using BankingApi.Domain.Exceptions;
using FluentValidation;

namespace BankingApi.Application.Transfers;

/// <summary>
/// Transfer use cases.
/// </summary>
public interface ITransferService
{
    /// <summary>
    /// Transfers money between two accounts in a single database transaction. Repeating a request with the same
    /// idempotency key returns the original result without moving money again.
    /// </summary>
    /// <param name="request">The transfer details.</param>
    /// <param name="idempotencyKey">The client-supplied idempotency key (required).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The transfer, and whether it was a replay of an earlier request.</returns>
    /// <exception cref="ValidationException">Thrown when the request or idempotency key is invalid.</exception>
    /// <exception cref="NotFoundException">Thrown when either account does not exist.</exception>
    /// <exception cref="IdempotencyKeyReusedException">Thrown when the key was used for a different request.</exception>
    /// <exception cref="DailyTransferLimitExceededException">Thrown when the source account's daily limit would be exceeded.</exception>
    /// <exception cref="DomainException">Thrown when another business rule is violated (for example, insufficient funds).</exception>
    /// <exception cref="ConcurrencyConflictException">Thrown when either account was changed by another request.</exception>
    Task<TransferResult> TransferAsync(
        TransferRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default);
}
