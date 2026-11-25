using BankingApi.Application.Common.Validation;
using FluentValidation;

namespace BankingApi.Application.Transactions;

/// <summary>
/// Validates <see cref="TransactionHistoryQuery"/>.
/// </summary>
public sealed class TransactionHistoryQueryValidator : PageRequestValidatorBase<TransactionHistoryQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionHistoryQueryValidator"/> class.
    /// </summary>
    public TransactionHistoryQueryValidator()
    {
        RuleFor(query => query.To)
            .GreaterThan(query => query.From)
            .When(query => query.From.HasValue && query.To.HasValue)
            .WithMessage("'To' must be later than 'From'.");
    }
}
