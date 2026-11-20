using System.Linq.Expressions;
using Craft.Core.Pagination;
using Craft.Data;
using Craft.Domain.Abstractions;
using Craft.QuerySpec;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Craft.Repositories;

public class QueryRepository<TEntity, TKey, TContext> : Repository<TEntity, TKey, TContext>, IQueryReadRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : notnull
    where TContext : DbContext, ICraftDbContext
{
    #region Fields

    private readonly int _maximumPageSize;
    private readonly int _maximumListSize;

    #endregion

    #region Constructors

    public QueryRepository(TContext context, IOptions<RepositoryOptions> options) : base(context, options)
    {
        _maximumPageSize = options.Value.MaximumPageSize;
        _maximumListSize = options.Value.MaximumListSize;
    }

    #endregion

    #region Methods

    private IQueryable<TEntity> Filter(Query<TEntity> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return query.ApplyFilters(EntityQuery).AsNoTracking();
    }

    private IQueryable<TEntity> Rows(Query<TEntity> query)
    {
        IQueryable<TEntity> source = WithDetails(Filter(query), query.IncludeDetails);
        foreach (string path in query.Includes)
            source = source.Include(path);
        return query.ApplyOrdering(source, entity => entity.Id);
    }

    public virtual Task<TEntity?> SingleAsync(Query<TEntity> query, CancellationToken cancellationToken = default)
        => Rows(query).SingleOrDefaultAsync(cancellationToken);

    public virtual Task<TEntity?> FirstAsync(Query<TEntity> query, CancellationToken cancellationToken = default)
        => Rows(query).FirstOrDefaultAsync(cancellationToken);

    public virtual Task<long> CountAsync(Query<TEntity> query, CancellationToken cancellationToken = default)
        => Filter(query).LongCountAsync(cancellationToken);

    public virtual Task<bool> AnyAsync(Query<TEntity> query, CancellationToken cancellationToken = default)
        => Filter(query).AnyAsync(cancellationToken);

    public virtual async Task<IReadOnlyList<TResult>> ListAsync<TResult>(Query<TEntity> query,
        Expression<Func<TEntity, TResult>> projection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projection);
        List<TResult> items = await Rows(query).Select(projection).Take(_maximumListSize + 1)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (items.Count > _maximumListSize)
            throw new InvalidOperationException("Result exceeds MaximumListSize. Use paging.");
        return items.AsReadOnly();
    }

    public virtual async Task<Page<TResult>> PageAsync<TResult>(Query<TEntity> query,
        Expression<Func<TEntity, TResult>> projection, long page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, _maximumPageSize);
        PageInfo requested = new(page, pageSize, 0);
        if (requested.Offset > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(page), "EF offset exceeds Int32.MaxValue.");

        long total = await CountAsync(query, cancellationToken).ConfigureAwait(false);
        List<TResult> items = await Rows(query).Skip((int)requested.Offset).Take(pageSize).Select(projection)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (items.Count != 0)
            total = Math.Max(total, requested.Offset + items.Count);

        return new Page<TResult>(items, new PageInfo(page, pageSize, total));
    }
    #endregion
}

public class QueryRepository<TEntity, TContext>(TContext context, IOptions<RepositoryOptions> options)
    : QueryRepository<TEntity, KeyType, TContext>(context, options), IQueryReadRepository<TEntity>, IRepository<TEntity>
    where TEntity : class, IEntity
    where TContext : DbContext, ICraftDbContext;
