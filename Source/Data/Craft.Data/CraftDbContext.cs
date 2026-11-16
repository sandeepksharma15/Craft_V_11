using Microsoft.EntityFrameworkCore;

namespace Craft.Data;

public interface ICraftDbContext
{
    CraftPersistence Persistence { get; }
}

public abstract class CraftDbContext : DbContext, ICraftDbContext
{
    protected CraftDbContext(DbContextOptions options) : base(options) => Persistence = new(this, options);

    public CraftPersistence Persistence { get; }

    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureModel(modelBuilder);
        Persistence.ConfigureModel(modelBuilder);
    }

    protected virtual void ConfigureModel(ModelBuilder modelBuilder) { }

    public sealed override int SaveChanges(bool acceptAllChangesOnSuccess)
        => Persistence.SaveChanges(acceptAllChangesOnSuccess, () => base.SaveChanges(true));

    public sealed override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        => Persistence.SaveChangesAsync(acceptAllChangesOnSuccess, token => base.SaveChangesAsync(true, token), cancellationToken);
}
