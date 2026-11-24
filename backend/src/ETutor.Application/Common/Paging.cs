using Microsoft.EntityFrameworkCore;

namespace ETutor.Application.Common;

/// <summary>Page request from the query string. Values are clamped, never rejected: page ≥ 1, size 1..50.</summary>
public sealed record PageRequest(int Page = 1, int PageSize = PageRequest.DefaultPageSize)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;

    public int SafePage => Math.Max(1, Page);

    public int SafePageSize => Math.Clamp(PageSize, 1, MaxPageSize);
}

/// <summary>The paginated envelope payload: <c>{ items, page, pageSize, totalCount, totalPages }</c>.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public static class PagingExtensions
{
    /// <summary>Counts, then fetches one page. <paramref name="query"/> must already be ordered.</summary>
    public static async Task<PagedResult<T>> ToPagedAsync<T>(
        this IQueryable<T> query, PageRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.SafePage - 1) * request.SafePageSize)
            .Take(request.SafePageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<T>(items, request.SafePage, request.SafePageSize, total);
    }

    public static PagedResult<TOut> Map<TIn, TOut>(this PagedResult<TIn> page, Func<TIn, TOut> map)
    {
        ArgumentNullException.ThrowIfNull(page);
        return new PagedResult<TOut>(page.Items.Select(map).ToList(), page.Page, page.PageSize, page.TotalCount);
    }
}
