using BankingApi.Domain.Entities;
using FluentValidation;

namespace BankingApi.Application.Customers;

/// <summary>
/// FluentValidation rules for customer fields, shared by customer creation and registration.
/// </summary>
public static class CustomerRules
{
    /// <summary>Requires a full name within the stored length.</summary>
    /// <typeparam name="T">The validated type.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <returns>The rule builder, for chaining.</returns>
    public static IRuleBuilderOptions<T, string> ValidFullName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(Customer.FullNameMaxLength);

    /// <summary>Requires a well-formed email address within the stored length.</summary>
    /// <typeparam name="T">The validated type.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <returns>The rule builder, for chaining.</returns>
    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(Customer.EmailMaxLength).EmailAddress();

    /// <summary>Requires a national ID made of letters, digits and hyphens.</summary>
    /// <typeparam name="T">The validated type.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <returns>The rule builder, for chaining.</returns>
    public static IRuleBuilderOptions<T, string> ValidNationalId<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MaximumLength(Customer.NationalIdMaxLength)
            .Matches("^[A-Za-z0-9-]+$")
            .WithMessage("'{PropertyName}' may contain only letters, digits and hyphens.");
}
