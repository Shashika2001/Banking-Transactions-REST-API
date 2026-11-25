using BankingApi.Domain.Entities;
using FluentValidation;

namespace BankingApi.Application.Customers;

/// <summary>
/// Validates <see cref="CreateCustomerRequest"/>.
/// </summary>
public sealed class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateCustomerRequestValidator"/> class.
    /// </summary>
    public CreateCustomerRequestValidator()
    {
        RuleFor(request => request.FullName).NotEmpty().MaximumLength(Customer.FullNameMaxLength);
        RuleFor(request => request.Email).NotEmpty().MaximumLength(Customer.EmailMaxLength).EmailAddress();
        RuleFor(request => request.NationalId)
            .NotEmpty()
            .MaximumLength(Customer.NationalIdMaxLength)
            .Matches("^[A-Za-z0-9-]+$")
            .WithMessage("'{PropertyName}' may contain only letters, digits and hyphens.");
    }
}
