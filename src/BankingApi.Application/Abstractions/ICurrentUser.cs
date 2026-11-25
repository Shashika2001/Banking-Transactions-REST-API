namespace BankingApi.Application.Abstractions;

/// <summary>
/// Provides the identity of the user making the current request.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Gets an identifier for the current user, used in audit records. Unauthenticated callers are reported as
    /// <c>anonymous</c>.
    /// </summary>
    string UserId { get; }

    /// <summary>Gets a value indicating whether the caller presented a valid token.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Gets a value indicating whether the caller has the Admin role.</summary>
    bool IsAdmin { get; }

    /// <summary>Gets the customer the caller signs in as, or <see langword="null"/> for admins and anonymous callers.</summary>
    Guid? CustomerId { get; }
}
