using System.Linq.Expressions;

namespace Craft.QuerySpec;

/// <summary>An immutable server-side specification. Never deserialize expressions from HTTP requests.</summary>
public sealed class Query<T> where T : class
{
    #region Fields

    private readonly Func<IQueryable<T>, IOrderedQueryable<T>>? _ordering;

    #endregion

    #region Constructors

    public Query() : this([], [], null, false) { }

    private Query(IReadOnlyList<Expression<Func<T, bool>>> predicates, IReadOnlyList<string> includes,
        Func<IQueryable<T>, IOrderedQueryable<T>>? ordering, bool includeDetails)
    {
        Predicates = predicates;
        Includes = includes;
        _ordering = ordering;
        IncludeDetails = includeDetails;
    }

    #endregion

    #region Properties

    public IReadOnlyList<Expression<Func<T, bool>>> Predicates { get; }

    public IReadOnlyList<string> Includes { get; }

    public bool IncludeDetails { get; }

    #endregion

    #region Methods

    public Query<T> Where(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return new Query<T>(Array.AsReadOnly([.. Predicates, predicate]), Includes, _ordering, IncludeDetails);
    }

    public Query<T> OrderBy<TValue>(Expression<Func<T, TValue>> selector, bool descending = false)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new Query<T>(Predicates, Includes, source =>
        {
            if (_ordering is null)
                return descending ? source.OrderByDescending(selector) : source.OrderBy(selector);

            IOrderedQueryable<T> ordered = _ordering(source);
            return descending ? ordered.ThenByDescending(selector) : ordered.ThenBy(selector);
        }, IncludeDetails);
    }

    /// <summary>Appends server-owned navigation paths; EF validates paths during execution.</summary>
    public Query<T> Include(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return new Query<T>(Predicates, Array.AsReadOnly([.. Includes, path]), _ordering, IncludeDetails);
    }

    public Query<T> WithDetails(bool includeDetails = true)
        => new(Predicates, Includes, _ordering, includeDetails);

    public IQueryable<T> ApplyFilters(IQueryable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        foreach (Expression<Func<T, bool>> predicate in Predicates)
            source = source.Where(predicate);
        return source;
    }

    /// <summary>Always appends a unique key to make paging deterministic.</summary>
    public IOrderedQueryable<T> ApplyOrdering<TKey>(IQueryable<T> source, Expression<Func<T, TKey>> uniqueKey)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(uniqueKey);
        return _ordering is null ? source.OrderBy(uniqueKey) : _ordering(source).ThenBy(uniqueKey);
    }
    #endregion
}
