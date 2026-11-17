using Craft.Data;
using Craft.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Craft.Repositories;

public sealed class RepositoryBuilder<TContext>(IServiceCollection services) where TContext : DbContext, ICraftDbContext
{
    private readonly IServiceCollection _services = services ?? throw new ArgumentNullException(nameof(services));

    public RepositoryBuilder<TContext> Add<TEntity, TKey>() where TEntity : class, IEntity<TKey> where TKey : notnull
        => AddCustom<TEntity, TKey, Repository<TEntity, TKey, TContext>>();

    public RepositoryBuilder<TContext> Add<TEntity>() where TEntity : class, IEntity
        => AddCustom<TEntity, Repository<TEntity, TContext>>();

    public RepositoryBuilder<TContext> AddCustom<TEntity, TKey, TRepository>()
        where TEntity : class, IEntity<TKey> where TKey : notnull where TRepository : Repository<TEntity, TKey, TContext>
    {
        if (_services.Any(d => d.ServiceType == typeof(IRepository<TEntity, TKey>) || d.ServiceType == typeof(IReadRepository<TEntity, TKey>)))
            throw new InvalidOperationException("The entity/key pair already has a repository registration. Use a custom interface for multiple context bindings.");
        _services.AddScoped<TRepository>();
        _services.AddScoped<IRepository<TEntity, TKey>>(provider => provider.GetRequiredService<TRepository>());
        _services.AddScoped<IReadRepository<TEntity, TKey>>(provider => provider.GetRequiredService<TRepository>());
        return this;
    }

    public RepositoryBuilder<TContext> AddCustom<TEntity, TRepository>()
        where TEntity : class, IEntity where TRepository : Repository<TEntity, TContext>
    {
        AddCustom<TEntity, KeyType, TRepository>();
        _services.AddScoped<IRepository<TEntity>>(provider => provider.GetRequiredService<TRepository>());
        _services.AddScoped<IReadRepository<TEntity>>(provider => provider.GetRequiredService<TRepository>());
        return this;
    }
}

public static class RepositoryRegistration
{
    public static IServiceCollection AddCraftRepositories<TContext>(this IServiceCollection services,
        Action<RepositoryBuilder<TContext>> configure, Action<RepositoryOptions>? options = null) where TContext : DbContext, ICraftDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        var registration = services.AddOptions<RepositoryOptions>().Validate(RepositoryOptions.IsValid, "Invalid repository limits.").ValidateOnStart();
        if (options is not null) registration.Configure(options);
        configure(new RepositoryBuilder<TContext>(services));
        return services;
    }
}
