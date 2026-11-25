using BankingApi.Application.Common.Validation;
using BankingApi.Domain.Entities;
using FluentValidation;

namespace BankingApi.Application.Accounts;

/// <summary>
/// Validates <see cref="OpenAccountRequest"/>.
/// </summary>
public sealed class OpenAccountRequestValidator : AbstractValidator<OpenAccountRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAccountRequestValidator"/> class.
    /// </summary>
    public OpenAccountRequestValidator()
    {
        RuleFor(request => request.CustomerId).NotEmpty();
        RuleFor(request => request.Currency)
            .NotEmpty()
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("'{PropertyName}' must be a three-letter ISO 4217 code.");
    }
}

/// <summary>
/// Validates <see cref="MoneyOperationRequest"/>.
/// </summary>
public sealed class MoneyOperationRequestValidator : AbstractValidator<MoneyOperationRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyOperationRequestValidator"/> class.
    /// </summary>
    public MoneyOperationRequestValidator()
    {
        RuleFor(request => request.Amount).ValidMoneyAmount();
        RuleFor(request => request.Reference).MaximumLength(Transaction.ReferenceMaxLength);
    }
}
