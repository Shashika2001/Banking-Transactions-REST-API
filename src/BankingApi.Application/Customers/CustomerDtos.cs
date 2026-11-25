namespace BankingApi.Application.Customers;

/// <summary>
/// Request to create a customer.
/// </summary>
/// <param name="FullName">The customer's full name.</param>
/// <param name="Email">The customer's email address.</param>
/// <param name="NationalId">The customer's national identification number.</param>
public sealed record CreateCustomerRequest(string FullName, string Email, string NationalId);

/// <summary>
/// A customer as returned by the API.
/// </summary>
/// <param name="Id">The customer identifier.</param>
/// <param name="FullName">The customer's full name.</param>
/// <param name="Email">The customer's email address.</param>
/// <param name="MaskedNationalId">The national ID with all but the last four characters masked.</param>
/// <param name="CreatedAt">When the customer was created (UTC).</param>
public sealed record CustomerResponse(Guid Id, string FullName, string Email, string MaskedNationalId, DateTimeOffset CreatedAt);
