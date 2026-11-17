using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Craft.Data;

/// <summary>One save coordinator shared by regular and Identity contexts. Failed units require a fresh context.</summary>
public sealed class CraftPersistence
{
    private readonly DbContext _context;
    private readonly IReadOnlyList<PersistenceFeature> _features;
    private readonly HashSet<object> _permanentDeletes = new(ReferenceEqualityComparer.Instance);
    private bool _failed;
    private bool _saving;

    public CraftPersistence(DbContext context, DbContextOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        _context = context;
        _features = options.FindExtension<CraftDataOptionsExtension>()?.Features ?? [];
        ModelKey = string.Join(";", _features.Select(f => f.GetType().AssemblyQualifiedName));
    }

    public string ModelKey { get; }

    internal bool IsPermanentDelete(object entity) => _permanentDeletes.Contains(entity);

    public void PermanentlyDelete<TEntity>(TEntity entity) where TEntity : class, Craft.Domain.Abstractions.ISoftDelete
    {
        ArgumentNullException.ThrowIfNull(entity);
        var entry = _context.Entry(entity);
        if (entry.State is EntityState.Detached or EntityState.Added || !entity.IsDeleted)
            throw new InvalidOperationException("Load a soft-deleted entity with IncludeDeleted before permanently deleting it.");
        _permanentDeletes.Add(entity);
        entry.State = EntityState.Deleted;
    }
    public TFeature? FindFeature<TFeature>() where TFeature : PersistenceFeature => _features.OfType<TFeature>().SingleOrDefault();

    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        if (!_features.Any(f => f is ITenantPersistenceFeature) && modelBuilder.Model.GetEntityTypes().Any(e =>
            e.ClrType.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Craft.Domain.Abstractions.IHasTenant<>))))
            throw new InvalidOperationException("Tenant entities require UseMultiTenancy; unscoped access is not implicit.");
        foreach (var feature in _features) feature.ConfigureModel(_context, modelBuilder);
    }

    private void EnsureCanSave(bool acceptAllChangesOnSuccess)
    {
        if (_failed) throw new InvalidOperationException("This context has a failed save. Dispose it and create a fresh context.");
        if (_saving) throw new InvalidOperationException("Nested saves are not supported.");
        if (!acceptAllChangesOnSuccess) throw new NotSupportedException("Craft saves require acceptAllChangesOnSuccess=true.");
        if (!_context.Database.IsRelational()) throw new NotSupportedException("Craft persistence requires a relational database.");
        if (_context.Database.CurrentTransaction is { SupportsSavepoints: false })
            throw new NotSupportedException("Existing transactions must support savepoints; disable SQL Server MARS.");
        if (_context.Database.CreateExecutionStrategy().RetriesOnFailure && ExecutionStrategy.Current is null)
            throw new NotSupportedException("Use the provider execution strategy around the complete unit, with a fresh context per attempt.");
    }

    private Action[] Prepare()
    {
        _context.ChangeTracker.DetectChanges();
        foreach (var feature in _features) feature.ValidateChanges(_context);
        foreach (var feature in _features.OrderBy(f => f.Stage)) feature.PrepareChanges(_context);
        return _features.Select(f => f.CaptureChanges(_context)).OfType<Action>().ToArray();
    }

    public int SaveChanges(bool acceptAllChangesOnSuccess, Func<int> save)
    {
        ArgumentNullException.ThrowIfNull(save);
        EnsureCanSave(acceptAllChangesOnSuccess);
        _saving = true;
        IDbContextTransaction? transaction = null;
        var owned = _context.Database.CurrentTransaction is null;
        var savepoint = "C" + Guid.NewGuid().ToString("N")[..31];
        var savepointCreated = false;
        try
        {
            var completions = Prepare();
            transaction = _context.Database.CurrentTransaction ?? _context.Database.BeginTransaction();
            if (!owned) { transaction.CreateSavepoint(savepoint); savepointCreated = true; }
            var count = save();
            foreach (var complete in completions) complete();
            if (completions.Length != 0) save();
            if (owned) transaction.Commit();
            else transaction.ReleaseSavepoint(savepoint);
            _permanentDeletes.Clear();
            return count;
        }
        catch
        {
            _failed = true;
            try
            {
                if (transaction is not null)
                {
                    if (owned) transaction.Rollback();
                    else if (savepointCreated) transaction.RollbackToSavepoint(savepoint);
                }
            }
            finally { _context.ChangeTracker.Clear(); }
            throw;
        }
        finally
        {
            _saving = false;
            if (owned) transaction?.Dispose();
        }
    }

    public async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, Func<CancellationToken, Task<int>> save,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(save);
        cancellationToken.ThrowIfCancellationRequested();
        EnsureCanSave(acceptAllChangesOnSuccess);
        _saving = true;
        IDbContextTransaction? transaction = null;
        var owned = _context.Database.CurrentTransaction is null;
        var savepoint = "C" + Guid.NewGuid().ToString("N")[..31];
        var savepointCreated = false;
        try
        {
            var completions = Prepare();
            transaction = _context.Database.CurrentTransaction ?? await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            if (!owned) { await transaction.CreateSavepointAsync(savepoint, cancellationToken).ConfigureAwait(false); savepointCreated = true; }
            var count = await save(cancellationToken).ConfigureAwait(false);
            foreach (var complete in completions) complete();
            if (completions.Length != 0) await save(cancellationToken).ConfigureAwait(false);
            if (owned) await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            else await transaction.ReleaseSavepointAsync(savepoint, cancellationToken).ConfigureAwait(false);
            _permanentDeletes.Clear();
            return count;
        }
        catch
        {
            _failed = true;
            try
            {
                if (transaction is not null)
                {
                    if (owned) await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                    else if (savepointCreated) await transaction.RollbackToSavepointAsync(savepoint, CancellationToken.None).ConfigureAwait(false);
                }
            }
            finally { _context.ChangeTracker.Clear(); }
            throw;
        }
        finally
        {
            _saving = false;
            if (owned && transaction is not null) await transaction.DisposeAsync().ConfigureAwait(false);
        }
    }
}
