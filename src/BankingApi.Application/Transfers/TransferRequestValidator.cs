using BankingApi.Application.Common.Validation;
using BankingApi.Domain.Entities;
using FluentValidation;

namespace BankingApi.Application.Transfers;

/// <summary>
/// Validates <see cref="TransferRequest"/>.
/// </summary>
public sealed class TransferRequestValidator : AbstractValidator<TransferRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransferRequestValidator"/> class.
    /// </summary>
    public TransferRequestValidator()
    {
        RuleFor(request => request.SourceAccountId).NotEmpty();
        RuleFor(request => request.DestinationAccountId)
            .NotEmpty()
            .NotEqual(request => request.SourceAccountId)
            .WithMessage("The destination account must be different from the source account.");
        RuleFor(request => request.Amount).ValidMoneyAmount();
        RuleFor(request => request.Reference).MaximumLength(Transaction.ReferenceMaxLength);
    }
}
