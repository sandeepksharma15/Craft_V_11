namespace Craft.Core.Pagination;

public sealed class PageInfo
{
    #region Public Constructors

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

    #endregion Public Constructors

    #region Public Properties

    public long CurrentPage { get; }
    public long From => Offset >= TotalCount ? 0 : Offset + 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasPreviousPage => CurrentPage > 1;
    public long Offset { get; }
    public int PageSize { get; }
    public long To => From == 0 ? 0 : From + Math.Min(PageSize - 1L, TotalCount - From);
    public long TotalCount { get; }
    public long TotalPages => TotalCount / PageSize + (TotalCount % PageSize == 0 ? 0 : 1);

    #endregion Public Properties
}
