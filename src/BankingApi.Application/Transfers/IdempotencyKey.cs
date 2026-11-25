namespace BankingApi.Application.Transfers;

/// <summary>
/// Constants for the idempotency key supplied with transfer requests.
/// </summary>
public static class IdempotencyKey
{
    /// <summary>The HTTP header carrying the idempotency key.</summary>
    public const string HeaderName = "Idempotency-Key";
}
