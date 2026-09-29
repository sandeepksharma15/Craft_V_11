using Microsoft.EntityFrameworkCore;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Linq.Expressions;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class QueryableFilterExtensions
{
    #region Public Methods

    /// <summary>
    /// Applies a query filter to the specified source when a filter is defined for the
    /// corresponding <see cref="DbSet{T}" />.
    /// </summary>
    public static IQueryable<T> ApplyQueryFilter<T>(this IQueryable<T> queryable, DbSet<T> dbSet) where T : class
    {
        ArgumentNullException.ThrowIfNull(queryable);
        ArgumentNullException.ThrowIfNull(dbSet);

        Expression<Func<T, bool>>? filter = dbSet.GetQueryFilter();
        return filter is null ? queryable : queryable.Where(filter);
    }

    #endregion Public Methods
}
