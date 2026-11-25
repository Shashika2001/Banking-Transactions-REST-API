namespace BankingApi.Application.Common.Models;

/// <summary>
/// Paging parameters for list requests.
/// </summary>
public record PageRequest
{
    /// <summary>The default number of items per page.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>The largest allowed number of items per page.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Gets the 1-based page number. Defaults to 1.</summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>Gets the number of items per page (1–100). Defaults to 20.</summary>
    public int PageSize { get; init; } = DefaultPageSize;
}
