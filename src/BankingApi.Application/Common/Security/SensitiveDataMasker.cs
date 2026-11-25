namespace BankingApi.Application.Common.Security;

/// <summary>
/// Masks sensitive values before they leave the API.
/// </summary>
public static class SensitiveDataMasker
{
    /// <summary>The number of trailing characters left visible.</summary>
    public const int VisibleCharacters = 4;

    /// <summary>
    /// Masks all but the last four characters of a national ID (for example, <c>199012345678</c> becomes
    /// <c>********5678</c>). Values of four characters or fewer are masked completely.
    /// </summary>
    /// <param name="nationalId">The national ID.</param>
    /// <returns>The masked value.</returns>
    public static string MaskNationalId(string nationalId)
    {
        ArgumentNullException.ThrowIfNull(nationalId);

        return nationalId.Length <= VisibleCharacters
            ? new string('*', nationalId.Length)
            : string.Concat(new string('*', nationalId.Length - VisibleCharacters), nationalId[^VisibleCharacters..]);
    }
}
