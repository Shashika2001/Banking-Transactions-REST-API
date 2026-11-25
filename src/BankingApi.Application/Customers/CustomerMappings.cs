using BankingApi.Domain.Entities;

namespace BankingApi.Application.Customers;

/// <summary>
/// Maps customer entities to DTOs.
/// </summary>
internal static class CustomerMappings
{
    private const int VisibleNationalIdCharacters = 4;

    /// <summary>
    /// Maps a customer to its response DTO, masking the national ID.
    /// </summary>
    public static CustomerResponse ToResponse(this Customer customer) =>
        new(customer.Id, customer.FullName, customer.Email, MaskNationalId(customer.NationalId), customer.CreatedAt);

    private static string MaskNationalId(string nationalId) =>
        nationalId.Length <= VisibleNationalIdCharacters
            ? new string('*', nationalId.Length)
            : string.Concat(new string('*', nationalId.Length - VisibleNationalIdCharacters), nationalId[^VisibleNationalIdCharacters..]);
}
