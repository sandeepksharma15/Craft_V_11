using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Craft.Data;

public sealed class CraftDataBuilder
{
    private readonly List<Func<IServiceProvider, PersistenceFeature>> _features = [];

    public CraftDataBuilder AddFeature(Func<IServiceProvider, PersistenceFeature> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _features.Add(factory);
        return this;
    }

    public CraftDataBuilder UseSoftDelete() => AddFeature(_ => new SoftDeleteFeature());
    public CraftDataBuilder UseConcurrency() => AddFeature(_ => new ConcurrencyFeature());
    public CraftDataBuilder UseVersioning() => AddFeature(_ => new VersionFeature());

    internal PersistenceFeature[] Build(IServiceProvider services)
    {
        var features = _features.Select(factory => factory(services) ?? throw new InvalidOperationException("A persistence feature factory returned null.")).ToArray();
        if (features.Select(f => f.GetType()).Distinct().Count() != features.Length)
            throw new InvalidOperationException("Each persistence feature may be registered only once.");
        return features;
    }
}

public static class CraftDataRegistration
{
    public static IServiceCollection AddCraftDbContext<TContext>(this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureProvider, Action<CraftDataBuilder> configureFeatures)
        where TContext : DbContext, ICraftDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureProvider);
        ArgumentNullException.ThrowIfNull(configureFeatures);
        var features = new CraftDataBuilder();
        configureFeatures(features);
        return services.AddDbContext<TContext>((provider, options) =>
        {
            configureProvider(options);
            options.UseCraftData(features.Build(provider));
        });
    }
}
