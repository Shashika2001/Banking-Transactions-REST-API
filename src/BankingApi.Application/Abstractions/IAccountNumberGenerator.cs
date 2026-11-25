namespace BankingApi.Application.Abstractions;

/// <summary>
/// Generates candidate account numbers. Uniqueness is checked by the caller.
/// </summary>
public interface IAccountNumberGenerator
{
    /// <summary>
    /// Generates a new candidate account number.
    /// </summary>
    /// <returns>An account number.</returns>
    string Generate();
}
