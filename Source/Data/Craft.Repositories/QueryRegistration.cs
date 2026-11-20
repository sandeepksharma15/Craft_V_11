using Craft.Data;
using Craft.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Craft.Repositories;

public static class QueryRegistration
{
    extension<TContext>(RepositoryBuilder<TContext> builder) where TContext : DbContext, ICraftDbContext
    {
        public RepositoryBuilder<TContext> AddQuery<TEntity, TKey>()
            where TEntity : class, IEntity<TKey>
            where TKey : notnull
            => builder.AddQueryCustom<TEntity, TKey, QueryRepository<TEntity, TKey, TContext>>();

        public RepositoryBuilder<TContext> AddQueryCustom<TEntity, TKey, TRepository>()
            where TEntity : class, IEntity<TKey>
            where TKey : notnull
            where TRepository : QueryRepository<TEntity, TKey, TContext>
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.AddCustom<TEntity, TKey, TRepository>();
            builder.Services.AddScoped<IQueryReadRepository<TEntity, TKey>>(provider => provider.GetRequiredService<TRepository>());
            return builder;
        }

        public RepositoryBuilder<TContext> AddQuery<TEntity>()
            where TEntity : class, IEntity
        {
            builder.AddQueryCustom<TEntity, KeyType, QueryRepository<TEntity, TContext>>();
            builder.Services.AddScoped<IQueryReadRepository<TEntity>>(provider => provider.GetRequiredService<QueryRepository<TEntity, TContext>>());
            builder.Services.AddScoped<IRepository<TEntity>>(provider => provider.GetRequiredService<QueryRepository<TEntity, TContext>>());
            builder.Services.AddScoped<IReadRepository<TEntity>>(provider => provider.GetRequiredService<QueryRepository<TEntity, TContext>>());
            return builder;
        }
    }
}
