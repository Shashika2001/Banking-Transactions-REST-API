namespace BankingApi.Application.Abstractions;

/// <summary>
/// Provides the identity of the user making the current request.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Gets an identifier for the current user, used in audit records.
    /// </summary>
    string UserId { get; }
}
