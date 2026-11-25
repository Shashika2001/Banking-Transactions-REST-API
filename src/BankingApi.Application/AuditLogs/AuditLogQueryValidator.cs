using BankingApi.Application.Common.Validation;
using BankingApi.Domain.Entities;
using FluentValidation;

namespace BankingApi.Application.AuditLogs;

/// <summary>
/// Validates <see cref="AuditLogQuery"/>.
/// </summary>
public sealed class AuditLogQueryValidator : PageRequestValidatorBase<AuditLogQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogQueryValidator"/> class.
    /// </summary>
    public AuditLogQueryValidator()
    {
        RuleFor(query => query.UserId).MaximumLength(AuditLog.UserIdMaxLength);
        RuleFor(query => query.Action).MaximumLength(AuditLog.ActionMaxLength);
        RuleFor(query => query.EntityName).MaximumLength(AuditLog.EntityNameMaxLength);
        RuleFor(query => query.EntityId).MaximumLength(AuditLog.EntityIdMaxLength);
        RuleFor(query => query.To)
            .GreaterThan(query => query.From)
            .When(query => query.From.HasValue && query.To.HasValue)
            .WithMessage("'To' must be later than 'From'.");
    }
}
