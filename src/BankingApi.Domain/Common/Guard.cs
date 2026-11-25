using BankingApi.Domain.Exceptions;

namespace BankingApi.Domain.Common;

/// <summary>
/// Guard clauses that enforce domain invariants and throw <see cref="DomainValidationException"/> on failure.
/// </summary>
internal static class Guard
{
    /// <summary>
    /// Ensures a required text value is present and within the maximum length.
    /// </summary>
    /// <returns>The trimmed value.</returns>
    public static string RequiredText(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        return MaxLength(value.Trim(), fieldName, maxLength);
    }

    /// <summary>
    /// Ensures an optional text value, when present, is not blank and is within the maximum length.
    /// </summary>
    /// <returns>The trimmed value, or <see langword="null"/> when no value was supplied.</returns>
    public static string? OptionalText(string? value, string fieldName, int maxLength)
    {
        if (value is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} must not be blank when supplied.");
        }

        return MaxLength(value.Trim(), fieldName, maxLength);
    }

    /// <summary>
    /// Ensures a <see cref="Guid"/> identifier is not empty.
    /// </summary>
    public static Guid NotEmpty(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException($"{fieldName} must not be empty.");
        }

        return value;
    }

    private static string MaxLength(string value, string fieldName, int maxLength)
    {
        if (value.Length > maxLength)
        {
            throw new DomainValidationException($"{fieldName} must be at most {maxLength} characters.");
        }

        return value;
    }
}
