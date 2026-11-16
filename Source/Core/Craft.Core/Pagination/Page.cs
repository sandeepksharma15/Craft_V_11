namespace Craft.Core.Pagination;

public sealed class Page<T>
{
    #region Public Constructors

    public Page(IReadOnlyList<T> items, PageInfo pagination)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(pagination);

        var snapshot = items.ToArray();
        var available = Math.Min(pagination.PageSize, Math.Max(0, pagination.TotalCount - pagination.Offset));

        if (snapshot.LongLength > available)
            throw new ArgumentException("Items exceed the available page range.", nameof(items));

        Items = Array.AsReadOnly(snapshot);
        Pagination = pagination;
    }

    #endregion Public Constructors

    #region Public Properties

    public IReadOnlyList<T> Items { get; }
    public PageInfo Pagination { get; }

    #endregion Public Properties
}
