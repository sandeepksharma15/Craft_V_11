namespace Craft.Core;

public sealed class PageInfo
{
    public long CurrentPage { get; }
    public int PageSize { get; }
    public long TotalCount { get; }
    public long Offset { get; }
    public long TotalPages => TotalCount / PageSize + (TotalCount % PageSize == 0 ? 0 : 1);
    public long From => Offset >= TotalCount ? 0 : Offset + 1;
    public long To => From == 0 ? 0 : From + Math.Min(PageSize - 1L, TotalCount - From);
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public PageInfo(long currentPage, int pageSize, long totalCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(currentPage, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);
        Offset = checked((currentPage - 1) * pageSize);
        CurrentPage = currentPage;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}