using System.Linq.Expressions;
using Craft.Expressions.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Craft.Expressions.Linq;

public static class QueryableFilterExtensions
{
    /// <summary>
    /// Applies a query filter to the specified source when a filter is defined for the corresponding <see cref="DbSet{T}"/>.
    /// </summary>
    public static IQueryable<T> ApplyQueryFilter<T>(this IQueryable<T> queryable, DbSet<T> dbSet) where T : class
    {
        ArgumentNullException.ThrowIfNull(queryable);
        ArgumentNullException.ThrowIfNull(dbSet);

        Expression<Func<T, bool>>? filter = dbSet.GetQueryFilter();
        return filter is null ? queryable : queryable.Where(filter);
    }
}
