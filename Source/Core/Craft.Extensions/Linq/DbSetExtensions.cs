using System.Linq.Expressions;
using Craft.Extensions.Expressions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.EntityFrameworkCore;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class DbSetExtensions
{
    extension<T>(DbSet<T> dbSet) where T : class
    {
        /// <summary>
        /// Retrieves the query filter expression configured for a given entity type within the current model.
        /// </summary>
        public Expression<Func<T, bool>>? GetQueryFilter()
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
        public IQueryable<T> RemoveFromQueryFilter(Expression<Func<T, bool>> condition)
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
}
