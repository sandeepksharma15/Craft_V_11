using System.Linq.Expressions;
using Craft.Core.Pagination;
using Craft.Domain.Abstractions;
using Craft.QuerySpec;
using Craft.Repositories;

namespace Craft.Testing.Repositories;

public abstract class QueryRepositoryStub<TEntity, TKey> : RepositoryStub<TEntity, TKey>, IQueryReadRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : notnull
{
    public virtual Task<TEntity?> SingleAsync(Query<TEntity> query, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this query operation in the test double.");

    public virtual Task<TEntity?> FirstAsync(Query<TEntity> query, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this query operation in the test double.");

    public virtual Task<long> CountAsync(Query<TEntity> query, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this query operation in the test double.");

    public virtual Task<bool> AnyAsync(Query<TEntity> query, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this query operation in the test double.");

    public virtual Task<IReadOnlyList<TResult>> ListAsync<TResult>(Query<TEntity> query,
        Expression<Func<TEntity, TResult>> projection, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this query operation in the test double.");

    public virtual Task<Page<TResult>> PageAsync<TResult>(Query<TEntity> query,
        Expression<Func<TEntity, TResult>> projection, long page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this query operation in the test double.");
}

public abstract class QueryRepositoryStub<TEntity> : QueryRepositoryStub<TEntity, KeyType>, IQueryReadRepository<TEntity>, IRepository<TEntity>
    where TEntity : class, IEntity;
