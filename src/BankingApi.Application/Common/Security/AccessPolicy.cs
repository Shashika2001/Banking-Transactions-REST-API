using BankingApi.Application.Abstractions;
using BankingApi.Application.Common.Exceptions;
using BankingApi.Domain.Entities;

namespace BankingApi.Application.Common.Security;

/// <summary>
/// Default <see cref="IAccessPolicy"/> based on the <see cref="ICurrentUser"/>.
/// </summary>
public sealed class AccessPolicy : IAccessPolicy
{
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessPolicy"/> class.
    /// </summary>
    /// <param name="currentUser">The caller.</param>
    public AccessPolicy(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public void EnsureAdmin()
    {
        if (!IsAuthenticatedAdmin)
        {
            throw new ForbiddenAccessException("This operation requires the Admin role.");
        }
    }

    /// <inheritdoc />
    public void EnsureCanAccessCustomer(Guid customerId)
    {
        if (!CanAccessCustomer(customerId))
        {
            throw new ForbiddenAccessException("You do not have access to this customer.");
        }
    }

    /// <inheritdoc />
    public void EnsureCanAccessAccount(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (!CanAccessCustomer(account.CustomerId))
        {
            throw new ForbiddenAccessException("You do not have access to this account.");
        }
    }

    private bool IsAuthenticatedAdmin => _currentUser.IsAuthenticated && _currentUser.IsAdmin;

    private bool CanAccessCustomer(Guid customerId) =>
        IsAuthenticatedAdmin ||
        (_currentUser.IsAuthenticated && _currentUser.CustomerId is { } ownCustomerId && ownCustomerId == customerId);
}
