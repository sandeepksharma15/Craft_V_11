using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Linq;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class QueryableExtensions
{
    extension<T>(IQueryable<T> queryable)
    {
        /// <summary>
        /// Determines whether the specified queryable source supports asynchronous enumeration.
        /// </summary>
        public bool SupportsAsync()
        {
            ArgumentNullException.ThrowIfNull(queryable);

            return queryable is IAsyncEnumerable<T>;
        }

        /// <summary>
        /// Asynchronously converts an <see cref="IQueryable{T}"/> to a <see cref="List{T}"/> when asynchronous query execution is available,
        /// otherwise falls back to synchronous enumeration.
        /// </summary>
        public async Task<List<T>> ToListSafeAsync(CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(queryable);

            return queryable.SupportsAsync()
                ? await queryable.ToListAsync(cancellationToken).ConfigureAwait(false)
                : [.. queryable];
        }

        /// <summary>
        /// Asynchronously returns the number of elements in the sequence as a <see cref="long"/> when asynchronous query execution is available,
        /// otherwise falls back to synchronous execution.
        /// </summary>
        public async Task<long> LongCountSafeAsync(CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(queryable);

            return queryable.SupportsAsync()
                ? await queryable.LongCountAsync(cancellationToken).ConfigureAwait(false)
                : queryable.LongCount();
        }

        /// <summary>
        /// Asynchronously returns the number of elements in the sequence when asynchronous query execution is available,
        /// otherwise falls back to synchronous execution.
        /// </summary>
        public async Task<long> CountSafeAsync(CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(queryable);

            return queryable.SupportsAsync()
                ? await queryable.CountAsync(cancellationToken).ConfigureAwait(false)
                : queryable.Count();
        }
    }

    extension<T>(IQueryable<T> queryable) where T : class
    {
        /// <summary>
        /// Conditionally includes or excludes automatically included navigation properties in a query.
        /// </summary>
        public IQueryable<T> IncludeDetails(bool includeDetails)
        {
            ArgumentNullException.ThrowIfNull(queryable);

            return includeDetails ? queryable : queryable.IgnoreAutoIncludes();
        }

        /// <summary>
        /// Applies a query filter to the specified source when a filter is defined for the corresponding <see cref="DbSet{T}"/>.
        /// </summary>
        public IQueryable<T> ApplyQueryFilter(DbSet<T> dbSet)
        {
            ArgumentNullException.ThrowIfNull(queryable);
            ArgumentNullException.ThrowIfNull(dbSet);

            Expression<Func<T, bool>>? filter = dbSet.GetQueryFilter();

            return filter is null ? queryable : queryable.Where(filter);
        }

        /// <summary>
        /// Conditionally includes a related entity in the query based on the specified condition.
        /// </summary>
        public IQueryable<T> IncludeIf<TProperty>(bool condition, Expression<Func<T, TProperty>> navigationPropertyPath)
        {
            ArgumentNullException.ThrowIfNull(queryable);
            ArgumentNullException.ThrowIfNull(navigationPropertyPath);

            return condition ? queryable.Include(navigationPropertyPath) : queryable;
        }

        /// <summary>
        /// Conditionally disables query filters applied to the source queryable.
        /// </summary>
        public IQueryable<T> IgnoreQueryFiltersIf(bool ignore)
        {
            ArgumentNullException.ThrowIfNull(queryable);

            return ignore ? queryable.IgnoreQueryFilters() : queryable;
        }
    }
}
