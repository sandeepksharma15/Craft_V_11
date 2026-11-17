using Craft.Core.Pagination;
using Craft.Domain.Abstractions;

namespace Craft.Repositories;

public interface IReadRepository<TEntity, TKey> where TEntity : class, IEntity<TKey> where TKey : notnull
{
    Task<TEntity?> GetAsync(TKey id, bool includeDetails = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(bool includeDetails = false, CancellationToken cancellationToken = default);
    Task<Page<TEntity>> GetPagedListAsync(long currentPage, int pageSize, bool includeDetails = false, CancellationToken cancellationToken = default);
    Task<long> GetCountAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}

public interface IReadRepository<TEntity> : IReadRepository<TEntity, KeyType> where TEntity : class, IEntity;
