namespace BankingApi.Application.Common.Models;

/// <summary>
/// One page of a larger result set.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">The items on this page.</param>
/// <param name="PageNumber">The 1-based page number.</param>
/// <param name="PageSize">The maximum number of items per page.</param>
/// <param name="TotalCount">The total number of items across all pages.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount)
{
    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Projects the items on this page, keeping the paging information.
    /// </summary>
    /// <typeparam name="TResult">The projected item type.</typeparam>
    /// <param name="selector">The projection.</param>
    /// <returns>A page of projected items.</returns>
    public PagedResult<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new PagedResult<TResult>(Items.Select(selector).ToList(), PageNumber, PageSize, TotalCount);
    }
}
