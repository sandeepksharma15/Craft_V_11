using System.Linq.Expressions;
using Craft.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Craft.Data;

public sealed class SoftDeleteFeature : PersistenceFeature
{
    public const string FilterName = "Craft.SoftDelete";

    public override void ConfigureModel(DbContext context, ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes().Where(e => typeof(ISoftDelete).IsAssignableFrom(e.ClrType)))
        {
            if (entity.IsOwned()) throw new InvalidOperationException("Owned types cannot independently implement ISoftDelete.");
            if (entity.BaseType is not null)
            {
                if (!typeof(ISoftDelete).IsAssignableFrom(entity.GetRootType().ClrType))
                    throw new InvalidOperationException("Soft deletion must be declared on the hierarchy root.");
                continue;
            }
            if (entity.GetReferencingForeignKeys().Any(f => f.DeleteBehavior is DeleteBehavior.Cascade or DeleteBehavior.ClientCascade or DeleteBehavior.SetNull or DeleteBehavior.ClientSetNull))
                throw new InvalidOperationException("Relationships to soft-deletable principals require Restrict or NoAction; cascades must be explicit.");
            var parameter = Expression.Parameter(entity.ClrType, "entity");
            var flag = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
            modelBuilder.Entity(entity.ClrType).HasQueryFilter(FilterName, Expression.Lambda(Expression.Not(flag), parameter));
            modelBuilder.Entity(entity.ClrType).Property(nameof(ISoftDelete.IsDeleted)).IsConcurrencyToken();
        }
    }

    public override void PrepareChanges(DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries<ISoftDelete>().Where(e => e.State == EntityState.Deleted).ToArray())
        {
            if (((ICraftDbContext)context).Persistence.IsPermanentDelete(entry.Entity)) continue;
            entry.State = EntityState.Unchanged;
            entry.Entity.IsDeleted = true;
            entry.Property(e => e.IsDeleted).IsModified = true;
        }
    }
}

public static class SoftDeleteExtensions
{
    extension<TEntity>(IQueryable<TEntity> query) where TEntity : class, ISoftDelete
    {
        public IQueryable<TEntity> IncludeDeleted() => query.IgnoreQueryFilters([SoftDeleteFeature.FilterName]);
    }

    extension(DbContext context)
    {
        public void Restore<TEntity>(TEntity entity) where TEntity : class, ISoftDelete
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(entity);
            var entry = context.Entry(entity);
            if (entry.State is EntityState.Detached or EntityState.Added or EntityState.Deleted)
                throw new InvalidOperationException("Load the deleted entity with IncludeDeleted before restoring it.");
            entity.IsDeleted = false;
            entry.Property(nameof(ISoftDelete.IsDeleted)).IsModified = true;
        }
    }
}
