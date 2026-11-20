using System.Linq.Expressions;
using Craft.Core.Pagination;
using Craft.Domain.Abstractions;
using Craft.QuerySpec;

namespace Craft.Repositories;

public interface IQueryReadRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : notnull
{
    Task<TEntity?> SingleAsync(Query<TEntity> query, CancellationToken cancellationToken = default);
    Task<TEntity?> FirstAsync(Query<TEntity> query, CancellationToken cancellationToken = default);
    Task<long> CountAsync(Query<TEntity> query, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Query<TEntity> query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TResult>> ListAsync<TResult>(Query<TEntity> query,
        Expression<Func<TEntity, TResult>> projection, CancellationToken cancellationToken = default);
    Task<Page<TResult>> PageAsync<TResult>(Query<TEntity> query, Expression<Func<TEntity, TResult>> projection,
        long page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}

public interface IQueryReadRepository<TEntity> : IQueryReadRepository<TEntity, KeyType> where TEntity : class, IEntity;
