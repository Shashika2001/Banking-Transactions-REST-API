using BankingApi.Domain.Common;
using BankingApi.Domain.Enums;
using BankingApi.Domain.Exceptions;

namespace BankingApi.Domain.Entities;

/// <summary>
/// A login for the API. Customer users are linked to exactly one <see cref="Customer"/>; admin users are not.
/// </summary>
public sealed class User : Entity
{
    /// <summary>Maximum length of <see cref="Email"/>.</summary>
    public const int EmailMaxLength = Customer.EmailMaxLength;

    /// <summary>Maximum length of <see cref="PasswordHash"/>.</summary>
    public const int PasswordHashMaxLength = 512;

    // Required by EF Core for materialization.
    private User()
    {
        Email = string.Empty;
        PasswordHash = string.Empty;
    }

    private User(string email, string passwordHash, UserRole role, Guid? customerId, DateTimeOffset createdAt)
    {
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        CustomerId = customerId;
        CreatedAt = createdAt;
    }

    /// <summary>Gets the email address used to sign in.</summary>
    public string Email { get; private set; }

    /// <summary>Gets the hashed password. The plain-text password is never stored.</summary>
    public string PasswordHash { get; private set; }

    /// <summary>Gets the user's role.</summary>
    public UserRole Role { get; private set; }

    /// <summary>Gets the customer this user signs in as, or <see langword="null"/> for admins.</summary>
    public Guid? CustomerId { get; private set; }

    /// <summary>Gets when the user was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Creates a user with the <see cref="UserRole.Customer"/> role, linked to a customer.
    /// </summary>
    /// <param name="email">The sign-in email address.</param>
    /// <param name="passwordHash">The hashed password.</param>
    /// <param name="customerId">The linked customer.</param>
    /// <param name="createdAt">When the user was created.</param>
    /// <returns>The new user.</returns>
    /// <exception cref="DomainValidationException">Thrown when any value is missing or too long.</exception>
    public static User CreateCustomerUser(string email, string passwordHash, Guid customerId, DateTimeOffset createdAt) =>
        new(
            Guard.RequiredText(email, nameof(Email), EmailMaxLength),
            Guard.RequiredText(passwordHash, nameof(PasswordHash), PasswordHashMaxLength),
            UserRole.Customer,
            Guard.NotEmpty(customerId, nameof(CustomerId)),
            createdAt);

    /// <summary>
    /// Creates a user with the <see cref="UserRole.Admin"/> role.
    /// </summary>
    /// <param name="email">The sign-in email address.</param>
    /// <param name="passwordHash">The hashed password.</param>
    /// <param name="createdAt">When the user was created.</param>
    /// <returns>The new user.</returns>
    /// <exception cref="DomainValidationException">Thrown when any value is missing or too long.</exception>
    public static User CreateAdmin(string email, string passwordHash, DateTimeOffset createdAt) =>
        new(
            Guard.RequiredText(email, nameof(Email), EmailMaxLength),
            Guard.RequiredText(passwordHash, nameof(PasswordHash), PasswordHashMaxLength),
            UserRole.Admin,
            null,
            createdAt);

    /// <summary>
    /// Replaces the stored password hash (for example, when re-hashing with stronger settings after sign-in).
    /// </summary>
    /// <param name="passwordHash">The new hashed password.</param>
    /// <exception cref="DomainValidationException">Thrown when the hash is missing or too long.</exception>
    public void ChangePasswordHash(string passwordHash) =>
        PasswordHash = Guard.RequiredText(passwordHash, nameof(PasswordHash), PasswordHashMaxLength);
}
