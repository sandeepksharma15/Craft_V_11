using Craft.Data;
using Craft.Domain.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Craft.Identity.EntityFrameworkCore;

public abstract class CraftIdentityDbContext<TUser, TRole, TKey> : IdentityDbContext<TUser, TRole, TKey>, ICraftDbContext
    where TUser : IdentityUser<TKey>
    where TRole : IdentityRole<TKey>
    where TKey : IEquatable<TKey>
{
    protected CraftIdentityDbContext(DbContextOptions options) : base(options) => Persistence = new(this, options);

    public CraftPersistence Persistence { get; }

    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (new[] { typeof(TUser), typeof(TRole) }.Any(t => t.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHasTenant<>))))
            throw new NotSupportedException("Tenant-scoped Identity accounts require dedicated stores and uniqueness policies; this adapter supports global accounts.");
        base.OnModelCreating(modelBuilder);
        if (Persistence.FindFeature<SoftDeleteFeature>() is not null)
            foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys())
                .Where(f => typeof(ISoftDelete).IsAssignableFrom(f.PrincipalEntityType.ClrType)))
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        ConfigureModel(modelBuilder);
        Persistence.ConfigureModel(modelBuilder);
    }

    protected virtual void ConfigureModel(ModelBuilder modelBuilder) { }

    public sealed override int SaveChanges(bool acceptAllChangesOnSuccess)
        => Persistence.SaveChanges(acceptAllChangesOnSuccess, () => base.SaveChanges(true));

    public sealed override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        => Persistence.SaveChangesAsync(acceptAllChangesOnSuccess, token => base.SaveChangesAsync(true, token), cancellationToken);
}

public abstract class CraftIdentityDbContext : CraftIdentityDbContext<CraftUser, CraftRole, KeyType>
{
    protected CraftIdentityDbContext(DbContextOptions options) : base(options) { }
}
