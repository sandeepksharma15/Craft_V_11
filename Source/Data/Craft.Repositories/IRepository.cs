using Craft.Domain.Abstractions;

namespace Craft.Repositories;

public interface IRepository<TEntity, TKey> : IReadRepository<TEntity, TKey> where TEntity : class, IEntity<TKey> where TKey : notnull
{
    Task<TEntity?> LoadForUpdateAsync(TKey id, bool includeDetails = false, CancellationToken cancellationToken = default);
    Task<TEntity?> LoadDeletedAsync(TKey id, bool includeDetails = false, CancellationToken cancellationToken = default);
    Task<TEntity> AddAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default);
    Task<TEntity> UpdateAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> UpdateRangeAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default);
    Task<TEntity> DeleteAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> DeleteRangeAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default);
    Task<TEntity> RestoreAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> RestoreRangeAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default);
    Task<TEntity> PermanentlyDeleteAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default);
    /// <summary>Commits all pending changes in the shared context, including changes staged by other repositories.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IRepository<TEntity> : IRepository<TEntity, KeyType>, IReadRepository<TEntity> where TEntity : class, IEntity;
