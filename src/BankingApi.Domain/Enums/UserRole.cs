namespace BankingApi.Domain.Enums;

/// <summary>
/// The role of an API user.
/// </summary>
public enum UserRole
{
    /// <summary>A bank customer who may only access their own customer record and accounts.</summary>
    Customer = 1,

    /// <summary>A bank administrator who may access all data, including audit logs.</summary>
    Admin = 2,
}
