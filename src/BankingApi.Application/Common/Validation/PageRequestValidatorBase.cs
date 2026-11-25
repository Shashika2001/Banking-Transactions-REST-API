using BankingApi.Application.Common.Models;
using FluentValidation;

namespace BankingApi.Application.Common.Validation;

/// <summary>
/// Paging rules shared by every request derived from <see cref="PageRequest"/>.
/// </summary>
/// <typeparam name="T">The request type.</typeparam>
public abstract class PageRequestValidatorBase<T> : AbstractValidator<T>
    where T : PageRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PageRequestValidatorBase{T}"/> class.
    /// </summary>
    protected PageRequestValidatorBase()
    {
        RuleFor(request => request.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(request => request.PageSize).InclusiveBetween(1, PageRequest.MaxPageSize);
    }
}

/// <summary>
/// Validates a plain <see cref="PageRequest"/>.
/// </summary>
public sealed class PageRequestValidator : PageRequestValidatorBase<PageRequest>
{
}
