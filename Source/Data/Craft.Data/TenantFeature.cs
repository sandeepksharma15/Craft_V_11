using System.Linq.Expressions;
using Craft.Core.Abstractions;
using Craft.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Microsoft.Extensions.DependencyInjection;

namespace Craft.Data;

public interface ITenantPersistenceFeature;

public sealed class TenantFeature<TKey> : PersistenceFeature, ITenantPersistenceFeature where TKey : notnull
{
    public override PersistenceStage Stage => PersistenceStage.Ownership;

    public const string FilterName = "Craft.Tenant";
    private readonly TKey? _tenantId;
    private readonly bool _hasTenant;
    public TKey TenantId => _hasTenant ? _tenantId! : throw new InvalidOperationException("An established, non-default tenant is required before accessing tenant data.");

    public TenantFeature(ICurrentTenant<TKey> tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        _hasTenant = tenant.IsAvailable && tenant.TryGetId(out _tenantId) &&
            !EqualityComparer<TKey>.Default.Equals(_tenantId, default) &&
            !(_tenantId is string text && string.IsNullOrWhiteSpace(text));
    }

    public override void ConfigureModel(DbContext context, ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var contracts = entity.ClrType.GetInterfaces().Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IHasTenant<>)).ToArray();
            if (contracts.Length == 0) continue;
            if (contracts.Length != 1 || contracts[0] != typeof(IHasTenant<TKey>))
                throw new InvalidOperationException("Tenant entity key types must match the configured tenant feature.");
            if (entity.IsOwned()) throw new InvalidOperationException("Owned types inherit their owner's tenant; do not implement IHasTenant on them.");
            if (entity.BaseType is not null)
            {
                if (!typeof(IHasTenant<TKey>).IsAssignableFrom(entity.GetRootType().ClrType))
                    throw new InvalidOperationException("Tenant ownership must be declared on the hierarchy root.");
                continue;
            }
            foreach (var foreignKey in entity.GetForeignKeys().Where(f => typeof(IHasTenant<TKey>).IsAssignableFrom(f.PrincipalEntityType.ClrType)))
            {
                var index = foreignKey.Properties.ToList().FindIndex(p => p.Name == nameof(IHasTenant<>.TenantId));
                if (index < 0 || foreignKey.PrincipalKey.Properties[index].Name != nameof(IHasTenant<>.TenantId))
                    throw new InvalidOperationException("Relationships between tenant entities must include TenantId in both foreign and principal keys.");
            }
            var parameter = Expression.Parameter(entity.ClrType, "entity");
            var persistence = Expression.Property(Expression.Constant(context), nameof(ICraftDbContext.Persistence));
            var feature = Expression.Call(persistence, nameof(CraftPersistence.FindFeature), [typeof(TenantFeature<TKey>)]);
            var tenant = Expression.Property(feature, nameof(TenantId));
            modelBuilder.Entity(entity.ClrType).HasQueryFilter(FilterName, Expression.Lambda(
                Expression.Equal(Expression.Property(parameter, nameof(IHasTenant<>.TenantId)), tenant), parameter));
            var tenantProperty = modelBuilder.Entity(entity.ClrType).Property(nameof(IHasTenant<>.TenantId)).IsConcurrencyToken().IsRequired();
            if (tenantProperty.Metadata.IsKey())
                tenantProperty.ValueGeneratedOnAdd().HasValueGenerator((_, _) => new TenantKeyGenerator());
            if (typeof(TKey) == typeof(string) && entity.FindProperty(nameof(IHasTenant<>.TenantId))!.GetMaxLength() is null)
                modelBuilder.Entity(entity.ClrType).Property(nameof(IHasTenant<>.TenantId)).HasMaxLength(256);
            modelBuilder.Entity(entity.ClrType).HasIndex(nameof(IHasTenant<>.TenantId));
        }
    }

    private sealed class TenantKeyGenerator : ValueGenerator<TKey>
    {
        public override bool GeneratesTemporaryValues => false;

        public override TKey Next(EntityEntry entry)
            => ((ICraftDbContext)entry.Context).Persistence.FindFeature<TenantFeature<TKey>>()!.TenantId;
    }

    public override void ValidateChanges(DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries<IHasTenant<TKey>>().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var property = entry.Property(e => e.TenantId);
            var current = property.CurrentValue;
            if (entry.State == EntityState.Added && EqualityComparer<TKey>.Default.Equals(current, default)) continue;
            if (!EqualityComparer<TKey>.Default.Equals(current, TenantId) ||
                entry.State != EntityState.Added && !EqualityComparer<TKey>.Default.Equals(property.OriginalValue, TenantId))
                throw new InvalidOperationException("Tenant ownership cannot be changed or used across tenant boundaries.");
        }
    }

    public override void PrepareChanges(DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries<IHasTenant<TKey>>().Where(e => e.State == EntityState.Added))
            entry.Entity.TenantId = TenantId;
    }
}

public static class TenantRegistration
{
    extension(CraftDataBuilder builder)
    {
        public CraftDataBuilder UseMultiTenancy<TKey>() where TKey : notnull
            => builder.AddFeature(services => new TenantFeature<TKey>(services.GetRequiredService<ICurrentTenant<TKey>>()));
    }
}
