using BankingApi.Domain.Entities;

namespace BankingApi.Application.Accounts;

/// <summary>
/// Maps account entities to DTOs.
/// </summary>
internal static class AccountMappings
{
    /// <summary>
    /// Maps an account to its response DTO.
    /// </summary>
    public static AccountResponse ToResponse(this Account account) =>
        new(
            account.Id,
            account.AccountNumber,
            account.CustomerId,
            account.Balance,
            account.Currency,
            account.Status.ToString(),
            account.CreatedAt);

    /// <summary>
    /// Maps an account to a balance DTO.
    /// </summary>
    public static BalanceResponse ToBalanceResponse(this Account account, DateTimeOffset asOf) =>
        new(account.Id, account.AccountNumber, account.Balance, account.Currency, asOf);
}
