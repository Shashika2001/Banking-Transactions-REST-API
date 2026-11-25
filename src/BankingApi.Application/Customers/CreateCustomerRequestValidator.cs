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
        RuleFor(request => request.FullName).ValidFullName();
        RuleFor(request => request.Email).ValidEmail();
        RuleFor(request => request.NationalId).ValidNationalId();
    }
}
