using BankingApi.Application.Customers;
using FluentValidation;

namespace BankingApi.Application.Auth;

/// <summary>
/// Password length rules. Length is enforced rather than character classes, following NIST SP 800-63B.
/// </summary>
public static class PasswordRules
{
    /// <summary>The minimum password length.</summary>
    public const int MinimumLength = 12;

    /// <summary>The maximum password length (bounds hashing cost).</summary>
    public const int MaximumLength = 128;
}

/// <summary>
/// Validates <see cref="RegisterRequest"/>.
/// </summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterRequestValidator"/> class.
    /// </summary>
    public RegisterRequestValidator()
    {
        RuleFor(request => request.FullName).ValidFullName();
        RuleFor(request => request.Email).ValidEmail();
        RuleFor(request => request.NationalId).ValidNationalId();

        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(PasswordRules.MinimumLength)
            .MaximumLength(PasswordRules.MaximumLength);
    }
}

/// <summary>
/// Validates <see cref="LoginRequest"/>.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginRequestValidator"/> class.
    /// </summary>
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().MaximumLength(Domain.Entities.User.EmailMaxLength);
        RuleFor(request => request.Password).NotEmpty().MaximumLength(PasswordRules.MaximumLength);
    }
}
