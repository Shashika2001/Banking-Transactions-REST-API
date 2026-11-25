using System.Net.Mail;
using BankingApi.Domain.Common;
using BankingApi.Domain.Exceptions;

namespace BankingApi.Domain.Entities;

/// <summary>
/// A bank customer who can own accounts.
/// </summary>
public sealed class Customer : Entity
{
    /// <summary>Maximum length of <see cref="FullName"/>.</summary>
    public const int FullNameMaxLength = 200;

    /// <summary>Maximum length of <see cref="Email"/>.</summary>
    public const int EmailMaxLength = 256;

    /// <summary>Maximum length of <see cref="NationalId"/>.</summary>
    public const int NationalIdMaxLength = 32;

    // Required by EF Core for materialization.
    private Customer()
    {
        FullName = string.Empty;
        Email = string.Empty;
        NationalId = string.Empty;
    }

    private Customer(string fullName, string email, string nationalId, DateTimeOffset createdAt)
    {
        FullName = fullName;
        Email = email;
        NationalId = nationalId;
        CreatedAt = createdAt;
    }

    /// <summary>Gets the customer's full name.</summary>
    public string FullName { get; private set; }

    /// <summary>Gets the customer's email address.</summary>
    public string Email { get; private set; }

    /// <summary>Gets the customer's government-issued identification number.</summary>
    public string NationalId { get; private set; }

    /// <summary>Gets when the customer was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    /// <param name="fullName">The customer's full name.</param>
    /// <param name="email">The customer's email address.</param>
    /// <param name="nationalId">The customer's national identification number.</param>
    /// <param name="createdAt">When the customer was created.</param>
    /// <returns>The new customer.</returns>
    /// <exception cref="DomainValidationException">Thrown when any value is missing, too long or malformed.</exception>
    public static Customer Create(string fullName, string email, string nationalId, DateTimeOffset createdAt)
    {
        var validEmail = Guard.RequiredText(email, nameof(Email), EmailMaxLength);
        if (!MailAddress.TryCreate(validEmail, out var parsed) || parsed.Address != validEmail)
        {
            throw new DomainValidationException($"{nameof(Email)} '{validEmail}' is not a valid email address.");
        }

        return new Customer(
            Guard.RequiredText(fullName, nameof(FullName), FullNameMaxLength),
            validEmail,
            Guard.RequiredText(nationalId, nameof(NationalId), NationalIdMaxLength),
            createdAt);
    }
}
