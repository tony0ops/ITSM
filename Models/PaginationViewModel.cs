using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace WebApplication1.Models;

public sealed class PaginationViewModel
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public int TotalPages { get; init; } = 1;
    public int TotalCount { get; init; }
    public string PageParameterName { get; init; } = "PageNumber";
    public Dictionary<string, string> RouteValues { get; init; } = [];

    public int FirstItem => TotalCount == 0 ? 0 : (PageNumber - 1) * PageSize + 1;
    public int LastItem => Math.Min(PageNumber * PageSize, TotalCount);

    public Dictionary<string, string> GetRouteValues(int pageNumber)
    {
        var values = new Dictionary<string, string>(RouteValues, StringComparer.OrdinalIgnoreCase)
        {
            [PageParameterName] = pageNumber.ToString(),
            ["PageSize"] = PageSize.ToString()
        };
        return values;
    }

    public static PaginationViewModel Create(int totalCount, int pageNumber, int pageSize,
        string pageParameterName = "PageNumber", Dictionary<string, string>? routeValues = null)
    {
        pageSize = pageSize is 10 or 25 or 50 or 100 ? pageSize : 25;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        return new PaginationViewModel
        {
            TotalCount = totalCount,
            PageSize = pageSize,
            TotalPages = totalPages,
            PageNumber = Math.Clamp(pageNumber, 1, totalPages),
            PageParameterName = pageParameterName,
            RouteValues = routeValues ?? []
        };
    }

    public static Dictionary<string, string> ReadRouteValues(IQueryCollection query, params string[] pageKeys)
    {
        var excluded = pageKeys.Append("PageNumber").Append("PageSize").ToHashSet(StringComparer.OrdinalIgnoreCase);
        return query.Where(item => !excluded.Contains(item.Key))
            .ToDictionary(item => item.Key, item => item.Value.ToString());
    }
}

public sealed class PagedResult<T>
{
    public required List<T> Items { get; init; }
    public required PaginationViewModel Pagination { get; init; }

    public static async Task<PagedResult<T>> CreateAsync(IQueryable<T> orderedQuery, int pageNumber,
        int pageSize, string pageParameterName = "PageNumber",
        Dictionary<string, string>? routeValues = null, CancellationToken cancellationToken = default)
    {
        var count = await orderedQuery.CountAsync(cancellationToken);
        var pagination = PaginationViewModel.Create(count, pageNumber, pageSize, pageParameterName, routeValues);
        var items = await orderedQuery.Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<T> { Items = items, Pagination = pagination };
    }
}
