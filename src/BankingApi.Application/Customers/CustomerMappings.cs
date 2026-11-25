using BankingApi.Application.Common.Security;
using BankingApi.Domain.Entities;

namespace BankingApi.Application.Customers;

/// <summary>
/// Maps customer entities to DTOs.
/// </summary>
internal static class CustomerMappings
{
    /// <summary>
    /// Maps a customer to its response DTO. The national ID is always masked.
    /// </summary>
    public static CustomerResponse ToResponse(this Customer customer) =>
        new(
            customer.Id,
            customer.FullName,
            customer.Email,
            SensitiveDataMasker.MaskNationalId(customer.NationalId),
            customer.CreatedAt);
}
