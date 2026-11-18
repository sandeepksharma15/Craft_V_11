using Microsoft.EntityFrameworkCore;

namespace Craft.Data;

public interface ICraftDbContext
{
    #region Public Properties

    CraftPersistence Persistence { get; }

    #endregion Public Properties
}

public abstract class CraftDbContext : DbContext, ICraftDbContext
{
    #region Protected Constructors

    protected CraftDbContext(DbContextOptions options) : base(options)
        => Persistence = new(this, options);

    #endregion Protected Constructors

    #region Protected Methods

    protected virtual void ConfigureModel(ModelBuilder modelBuilder) { }

    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureModel(modelBuilder);
        Persistence.ConfigureModel(modelBuilder);
    }

    #endregion Protected Methods

    #region Public Properties

    public CraftPersistence Persistence { get; }

    #endregion Public Properties

    #region Public Methods

    public sealed override int SaveChanges(bool acceptAllChangesOnSuccess)
        => Persistence.SaveChanges(acceptAllChangesOnSuccess, () => base.SaveChanges(true));

    public sealed override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        => Persistence.SaveChangesAsync(acceptAllChangesOnSuccess, token => base.SaveChangesAsync(true, token), cancellationToken);

    #endregion Public Methods
}
