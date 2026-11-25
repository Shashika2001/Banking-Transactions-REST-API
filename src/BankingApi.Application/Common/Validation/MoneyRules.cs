using BankingApi.Domain.Entities;
using FluentValidation;

namespace BankingApi.Application.Common.Validation;

/// <summary>
/// Reusable FluentValidation rules for money amounts.
/// </summary>
public static class MoneyRules
{
    /// <summary>
    /// Requires a positive amount with at most two decimal places that fits the storage column.
    /// </summary>
    /// <typeparam name="T">The validated type.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <returns>The rule builder, for chaining.</returns>
    public static IRuleBuilderOptions<T, decimal> ValidMoneyAmount<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThan(0m)
            .LessThanOrEqualTo(Account.MaxMoneyValue)
            .PrecisionScale(18, Account.MoneyDecimalPlaces, ignoreTrailingZeros: true)
            .WithMessage("'{PropertyName}' must have at most 2 decimal places.");
}
