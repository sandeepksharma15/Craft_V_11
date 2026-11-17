using Craft.Data;
using Craft.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Craft.Repositories;

public class Repository<TEntity, TKey, TContext>(TContext context, IOptions<RepositoryOptions> options)
    : ReadRepository<TEntity, TKey, TContext>(context, options), IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey> where TKey : notnull where TContext : DbContext, ICraftDbContext
{
    public virtual Task<TEntity?> LoadForUpdateAsync(TKey id, bool includeDetails = false, CancellationToken cancellationToken = default)
        => WithDetails(EntityQuery, includeDetails).AsTracking().SingleOrDefaultAsync(HasId(id), cancellationToken);

    public virtual Task<TEntity?> LoadDeletedAsync(TKey id, bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        EnsureSoftDelete();
        return WithDetails(EntityQuery.IgnoreQueryFilters([SoftDeleteFeature.FilterName]), includeDetails).AsTracking()
            .Where(e => EF.Property<bool>(e, nameof(ISoftDelete.IsDeleted))).SingleOrDefaultAsync(HasId(id), cancellationToken);
    }

    private void EnsureSoftDelete()
    {
        if (!typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)) || Context.Persistence.FindFeature<SoftDeleteFeature>() is null)
            throw new NotSupportedException("This operation requires an ISoftDelete entity and the context's UseSoftDelete feature.");
    }

    private void Validate(TEntity entity, bool adding)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var state = Context.Entry(entity).State;
        if (adding ? state != EntityState.Detached : state is not (EntityState.Unchanged or EntityState.Modified))
            throw new InvalidOperationException(adding ? "Only detached new entities may be added." : "Load an existing entity for editing before changing it; detached updates and deleted/added entities are not supported.");
    }

    private TEntity[] Snapshot(IEnumerable<TEntity> entities, bool adding, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entities);
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = entities.Take(MaximumBatchSize + 1).ToArray();
        if (snapshot.Length > MaximumBatchSize) throw new ArgumentException("Batch exceeds MaximumBatchSize.", nameof(entities));
        var seen = new HashSet<TEntity>(ReferenceEqualityComparer.Instance);
        foreach (var entity in snapshot)
        {
            Validate(entity, adding);
            if (!seen.Add(entity)) throw new ArgumentException("A batch cannot contain the same entity reference twice.", nameof(entities));
        }
        return snapshot;
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Validate(entity, true);
        await Context.Set<TEntity>().AddAsync(entity, cancellationToken).ConfigureAwait(false);
        if (autoSave) await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity;
    }

    public virtual async Task<IReadOnlyList<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default)
    {
        var snapshot = Snapshot(entities, true, cancellationToken);
        await Context.Set<TEntity>().AddRangeAsync(snapshot, cancellationToken).ConfigureAwait(false);
        if (autoSave && snapshot.Length != 0) await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Array.AsReadOnly(snapshot);
    }

    private void Stage(TEntity entity, Change operation)
    {
        if (operation == Change.Update) Context.Entry(entity).DetectChanges();
        else if (operation == Change.Delete) Context.Remove(entity);
        else if (operation == Change.Restore) Context.Restore((ISoftDelete)entity);
        else Context.Persistence.PermanentlyDelete((ISoftDelete)entity);
    }

    private void ValidateOperation(Change operation)
    {
        if (operation is Change.Restore or Change.PermanentDelete || operation == Change.Delete && typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
            EnsureSoftDelete();
    }

    private async Task<TEntity> ChangeAsync(TEntity entity, Change operation, bool autoSave, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Validate(entity, false);
        ValidateOperation(operation);
        Stage(entity, operation);
        if (autoSave) await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity;
    }

    private async Task<IReadOnlyList<TEntity>> ChangeRangeAsync(IEnumerable<TEntity> entities, Change operation, bool autoSave, CancellationToken cancellationToken)
    {
        var snapshot = Snapshot(entities, false, cancellationToken);
        ValidateOperation(operation);
        foreach (var entity in snapshot) Stage(entity, operation);
        if (autoSave && snapshot.Length != 0) await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Array.AsReadOnly(snapshot);
    }

    private enum Change { Update, Delete, Restore, PermanentDelete }

    public virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Context.SaveChangesAsync(cancellationToken);
    public virtual Task<TEntity> UpdateAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default)
        => ChangeAsync(entity, Change.Update, autoSave, cancellationToken);
    public virtual Task<IReadOnlyList<TEntity>> UpdateRangeAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default)
        => ChangeRangeAsync(entities, Change.Update, autoSave, cancellationToken);
    public virtual Task<TEntity> DeleteAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default)
        => ChangeAsync(entity, Change.Delete, autoSave, cancellationToken);
    public virtual Task<IReadOnlyList<TEntity>> DeleteRangeAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default)
        => ChangeRangeAsync(entities, Change.Delete, autoSave, cancellationToken);
    public virtual Task<TEntity> RestoreAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default)
        => ChangeAsync(entity, Change.Restore, autoSave, cancellationToken);
    public virtual Task<IReadOnlyList<TEntity>> RestoreRangeAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default)
        => ChangeRangeAsync(entities, Change.Restore, autoSave, cancellationToken);
    public virtual Task<TEntity> PermanentlyDeleteAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default)
        => ChangeAsync(entity, Change.PermanentDelete, autoSave, cancellationToken);
}

public class Repository<TEntity, TContext>(TContext context, IOptions<RepositoryOptions> options)
    : Repository<TEntity, KeyType, TContext>(context, options), IRepository<TEntity>
    where TEntity : class, IEntity where TContext : DbContext, ICraftDbContext;
