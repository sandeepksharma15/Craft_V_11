using Microsoft.EntityFrameworkCore;

namespace Craft.Auditing;

/// <summary>Coordinates business writes and audit writes in one relational transaction.</summary>
public abstract class AuditedDbContext : DbContext
{
    private readonly AuditTrailFactory _audits;

    protected AuditedDbContext(DbContextOptions options, AuditTrailFactory audits) : base(options)
    {
        ArgumentNullException.ThrowIfNull(audits);
        _audits = audits;
    }

    public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureAuditing();
    }

    public sealed override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureSupported(acceptAllChangesOnSuccess);
        var pending = _audits.Capture(this);
        var transaction = Database.CurrentTransaction;
        var ownsTransaction = transaction is null;
        transaction ??= Database.BeginTransaction();
        var savepoint = "Audit_" + Guid.NewGuid().ToString("N");
        if (!ownsTransaction) transaction.CreateSavepoint(savepoint);
        try
        {
            var count = base.SaveChanges(true);
            AuditTrails.AddRange(pending.Select(p => p.Complete()));
            base.SaveChanges(true);
            if (ownsTransaction) transaction.Commit();
            else transaction.ReleaseSavepoint(savepoint);
            return count;
        }
        catch
        {
            if (ownsTransaction) transaction.Rollback();
            else transaction.RollbackToSavepoint(savepoint);
            ChangeTracker.Clear();
            throw;
        }
        finally
        {
            if (ownsTransaction) transaction.Dispose();
        }
    }

    public sealed override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnsureSupported(acceptAllChangesOnSuccess);
        cancellationToken.ThrowIfCancellationRequested();
        var pending = _audits.Capture(this);
        var transaction = Database.CurrentTransaction;
        var ownsTransaction = transaction is null;
        transaction ??= await Database.BeginTransactionAsync(cancellationToken);
        var savepoint = "Audit_" + Guid.NewGuid().ToString("N");
        if (!ownsTransaction) await transaction.CreateSavepointAsync(savepoint, cancellationToken);
        try
        {
            var count = await base.SaveChangesAsync(true, cancellationToken);
            AuditTrails.AddRange(pending.Select(p => p.Complete()));
            await base.SaveChangesAsync(true, cancellationToken);
            if (ownsTransaction) await transaction.CommitAsync(cancellationToken);
            else await transaction.ReleaseSavepointAsync(savepoint, cancellationToken);
            return count;
        }
        catch
        {
            if (ownsTransaction) await transaction.RollbackAsync(CancellationToken.None);
            else await transaction.RollbackToSavepointAsync(savepoint, CancellationToken.None);
            ChangeTracker.Clear();
            throw;
        }
        finally
        {
            if (ownsTransaction) await transaction.DisposeAsync();
        }
    }

    private void EnsureSupported(bool acceptAllChangesOnSuccess)
    {
        if (!acceptAllChangesOnSuccess)
            throw new NotSupportedException("Audited saves require acceptAllChangesOnSuccess=true. Use a fresh context after a failed save or transaction rollback.");
        if (!Database.IsRelational())
            throw new NotSupportedException("Transactional auditing requires a relational database.");
        if (Database.CurrentTransaction is { SupportsSavepoints: false })
            throw new NotSupportedException("An existing transaction must support savepoints.");
    }
}