namespace HuGuWeb.Api.Http;

/// <summary>
/// Page-based list contract for large ERP screens. Do not accept arbitrary SQL field names.
/// </summary>
public sealed record ListQuery(
    int Page = 1,
    int PageSize = ListQueryLimits.DefaultPageSize,
    string? Sort = null,
    ListSortDirection Direction = ListSortDirection.Asc,
    string? Search = null);

public enum ListSortDirection
{
    Asc = 0,
    Desc = 1
}

public static class ListQueryLimits
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;

    public static int ClampPage(int page) => page < 1 ? 1 : page;

    public static int ClampPageSize(int pageSize) =>
        pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
