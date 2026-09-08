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
        /// <returns><see langword="true"/> when the queryable can be enumerated asynchronously; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="queryable"/> is <see langword="null"/>.</exception>
        public bool SupportsAsync()
        {
            ArgumentNullException.ThrowIfNull(queryable);

            return queryable is IAsyncEnumerable<T>;
        }

        /// <summary>
        /// Asynchronously converts an <see cref="IQueryable{T}"/> to a <see cref="List{T}"/> when asynchronous query execution is available,
        /// otherwise falls back to synchronous enumeration.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to observe while waiting for the operation to complete.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the materialized list.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="queryable"/> is <see langword="null"/>.</exception>
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
        /// <param name="cancellationToken">The cancellation token to observe while waiting for the operation to complete.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the total number of elements.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="queryable"/> is <see langword="null"/>.</exception>
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
        /// <param name="cancellationToken">The cancellation token to observe while waiting for the operation to complete.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the total number of elements.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="queryable"/> is <see langword="null"/>.</exception>
        public async Task<long> CountSafeAsync(CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(queryable);

            return queryable.SupportsAsync()
                ? await queryable.CountAsync(cancellationToken).ConfigureAwait(false)
                : queryable.Count();
        }
    }
}
