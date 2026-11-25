using BankingApi.Domain.Common;
using BankingApi.Domain.Enums;

namespace BankingApi.Domain.Entities;

/// <summary>
/// An immutable record of a single money movement on an <see cref="Account"/>.
/// Transactions are only created by <see cref="Account"/> so the recorded balance is always consistent.
/// </summary>
public sealed class Transaction : Entity
{
    /// <summary>Maximum length of <see cref="Reference"/>.</summary>
    public const int ReferenceMaxLength = 140;

    /// <summary>Maximum length of <see cref="IdempotencyKey"/>.</summary>
    public const int IdempotencyKeyMaxLength = 64;

    // Required by EF Core for materialization.
    private Transaction()
    {
    }

    private Transaction(
        Guid accountId,
        TransactionType type,
        decimal amount,
        decimal balanceAfter,
        DateTimeOffset createdAt,
        string? reference,
        string? idempotencyKey)
    {
        AccountId = accountId;
        Type = type;
        Amount = amount;
        BalanceAfter = balanceAfter;
        CreatedAt = createdAt;
        Reference = reference;
        IdempotencyKey = idempotencyKey;
    }

    /// <summary>Gets the account the transaction belongs to.</summary>
    public Guid AccountId { get; private set; }

    /// <summary>Gets the kind of money movement.</summary>
    public TransactionType Type { get; private set; }

    /// <summary>Gets the (always positive) amount moved.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Gets the account balance immediately after this transaction.</summary>
    public decimal BalanceAfter { get; private set; }

    /// <summary>Gets an optional free-text reference.</summary>
    public string? Reference { get; private set; }

    /// <summary>Gets the optional client-supplied key used to prevent duplicate processing.</summary>
    public string? IdempotencyKey { get; private set; }

    /// <summary>Gets when the transaction happened (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Records a transaction. Only <see cref="Account"/> calls this, after it has applied its business rules.
    /// </summary>
    internal static Transaction Record(
        Guid accountId,
        TransactionType type,
        decimal amount,
        decimal balanceAfter,
        DateTimeOffset createdAt,
        string? reference,
        string? idempotencyKey)
    {
        return new Transaction(
            accountId,
            type,
            amount,
            balanceAfter,
            createdAt,
            Guard.OptionalText(reference, nameof(Reference), ReferenceMaxLength),
            Guard.OptionalText(idempotencyKey, nameof(IdempotencyKey), IdempotencyKeyMaxLength));
    }
}
