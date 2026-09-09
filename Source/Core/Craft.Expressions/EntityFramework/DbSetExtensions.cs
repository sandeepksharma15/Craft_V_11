using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Craft.Expressions;

public static class DbSetExtensions
{
    /// <summary>
    /// Retrieves the query filter expression configured for a given entity type within the current model.
    /// </summary>
    public static Expression<Func<T, bool>>? GetQueryFilter<T>(this DbSet<T> dbSet) where T : class
    {
        ArgumentNullException.ThrowIfNull(dbSet);

        IModel model = dbSet.GetService<IModel>();
        IEntityType? entityType = model.FindEntityType(typeof(T));
        IReadOnlyCollection<IQueryFilter>? declaredFilters = entityType?.GetDeclaredQueryFilters();

        return declaredFilters == null || declaredFilters.Count == 0
            ? null
            : declaredFilters.FirstOrDefault() is { Expression: Expression<Func<T, bool>> expression }
                ? expression
                : null;
    }

    /// <summary>
    /// Removes a specific condition from the existing query filter for a given DbSet, if present.
    /// </summary>
    public static IQueryable<T> RemoveFromQueryFilter<T>(this DbSet<T> dbSet, Expression<Func<T, bool>> condition) where T : class
    {
        ArgumentNullException.ThrowIfNull(dbSet);
        ArgumentNullException.ThrowIfNull(condition);

        Expression<Func<T, bool>>? queryFilter = dbSet.GetQueryFilter();
        if (queryFilter is null)
            return dbSet.IgnoreQueryFilters();

        Expression<Func<T, bool>>? newQueryFilter = queryFilter.RemoveCondition(condition);
        return newQueryFilter is null
            ? dbSet.IgnoreQueryFilters()
            : dbSet.IgnoreQueryFilters().Where(newQueryFilter);
    }
}
