using Craft.Core.Pagination;
using Craft.Domain.Abstractions;
using Craft.Repositories;

namespace Craft.Testing.Repositories;

/// <summary>Override only the operations a test configures. Unconfigured operations fail rather than pretend to persist.</summary>
public abstract class RepositoryStub<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class, IEntity<TKey> where TKey : notnull
{
    public virtual Task<TEntity?> GetAsync(TKey id, bool includeDetails = false, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this repository operation in the test double.");
    public virtual Task<IReadOnlyList<TEntity>> GetAllAsync(bool includeDetails = false, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this repository operation in the test double.");
    public virtual Task<Page<TEntity>> GetPagedListAsync(long currentPage, int pageSize, bool includeDetails = false, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this repository operation in the test double.");
    public virtual Task<long> GetCountAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this repository operation in the test double.");
    public virtual Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this repository operation in the test double.");
    public virtual Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this repository operation in the test double.");
    public virtual Task<TEntity?> LoadForUpdateAsync(TKey id, bool includeDetails = false, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this repository operation in the test double.");
    public virtual Task<TEntity?> LoadDeletedAsync(TKey id, bool includeDetails = false, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this repository operation in the test double.");
    public virtual Task<TEntity> AddAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this repository operation in the test double.");
    public virtual Task<IReadOnlyList<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this repository operation in the test double.");
    public virtual Task<TEntity> UpdateAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this repository operation in the test double.");
    public virtual Task<IReadOnlyList<TEntity>> UpdateRangeAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this repository operation in the test double.");
    public virtual Task<TEntity> DeleteAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this repository operation in the test double.");
    public virtual Task<IReadOnlyList<TEntity>> DeleteRangeAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this repository operation in the test double.");
    public virtual Task<TEntity> RestoreAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this repository operation in the test double.");
    public virtual Task<IReadOnlyList<TEntity>> RestoreRangeAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this repository operation in the test double.");
    public virtual Task<TEntity> PermanentlyDeleteAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this repository operation in the test double.");
    public virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Configure this repository operation in the test double.");
}

public abstract class RepositoryStub<TEntity> : RepositoryStub<TEntity, KeyType>, IRepository<TEntity> where TEntity : class, IEntity;
