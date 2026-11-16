using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Craft.Data;

internal sealed class CraftDataOptionsExtension(PersistenceFeature[] features) : IDbContextOptionsExtension
{
    public IReadOnlyList<PersistenceFeature> Features { get; } = Array.AsReadOnly(features);
    public DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);
    public void ApplyServices(IServiceCollection services) { }
    public void Validate(IDbContextOptions options) { }

    private sealed class ExtensionInfo(IDbContextOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;
        public override string LogFragment => "CraftData ";
        public override int GetServiceProviderHashCode() => 0;
        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => other is ExtensionInfo;
        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo) => debugInfo["Craft:Data"] = "1";
    }
}

public static class CraftDataOptionsExtensions
{
    public static DbContextOptionsBuilder UseCraftData(this DbContextOptionsBuilder options, params PersistenceFeature[] features)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(features);
        if (features.Any(f => f is null) || features.Select(f => f.GetType()).Distinct().Count() != features.Length)
            throw new ArgumentException("Features must be non-null and unique by type.", nameof(features));
        ((IDbContextOptionsBuilderInfrastructure)options).AddOrUpdateExtension(new CraftDataOptionsExtension([.. features]));
        options.ReplaceService<IModelCacheKeyFactory, CraftModelCacheKeyFactory>();
        return options;
    }
}

public sealed class CraftModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) => context is ICraftDbContext craft
        ? (context.GetType(), craft.Persistence.ModelKey, designTime)
        : (object)(context.GetType(), designTime);
}
