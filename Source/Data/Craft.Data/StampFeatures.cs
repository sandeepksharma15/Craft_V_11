using Craft.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Craft.Data;

public sealed class ConcurrencyFeature : PersistenceFeature
{
    public override void ConfigureModel(DbContext context, ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes().Where(e => typeof(IHasConcurrency).IsAssignableFrom(e.ClrType)))
            modelBuilder.Entity(entity.ClrType).Property(nameof(IHasConcurrency.ConcurrencyStamp)).IsConcurrencyToken().HasMaxLength(IHasConcurrency.MaxLength);
    }

    public override void PrepareChanges(DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries<IHasConcurrency>().Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            entry.Entity.ConcurrencyStamp = Guid.NewGuid().ToString("N");
            if (entry.State == EntityState.Modified) entry.Property(e => e.ConcurrencyStamp).IsModified = true;
        }
    }
}

public sealed class VersionFeature : PersistenceFeature
{
    public override void ConfigureModel(DbContext context, ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes().Where(e => typeof(IHasVersion).IsAssignableFrom(e.ClrType)))
            modelBuilder.Entity(entity.ClrType).Property(nameof(IHasVersion.Version)).IsConcurrencyToken();
    }

    public override void PrepareChanges(DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries<IHasVersion>().Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            entry.Entity.Version = entry.State == EntityState.Added ? 1 : checked(entry.Property(e => e.Version).OriginalValue + 1);
            if (entry.State == EntityState.Modified) entry.Property(e => e.Version).IsModified = true;
        }
    }
}
