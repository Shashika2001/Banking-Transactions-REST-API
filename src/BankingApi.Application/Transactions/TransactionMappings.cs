using BankingApi.Domain.Entities;

namespace BankingApi.Application.Transactions;

/// <summary>
/// Maps transaction entities to DTOs.
/// </summary>
internal static class TransactionMappings
{
    /// <summary>
    /// Maps a transaction to its response DTO.
    /// </summary>
    public static TransactionResponse ToResponse(this Transaction transaction) =>
        new(
            transaction.Id,
            transaction.AccountId,
            transaction.Type.ToString(),
            transaction.Amount,
            transaction.BalanceAfter,
            transaction.Reference,
            transaction.TransferId,
            transaction.CreatedAt);
}
