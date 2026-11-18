using System.Linq.Expressions;
using Craft.Core.Pagination;
using Craft.Data;
using Craft.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Craft.Repositories;

public class ReadRepository<TEntity, TKey, TContext> : IReadRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey> where TKey : notnull where TContext : DbContext, ICraftDbContext
{
    #region Private Fields

    private readonly int _maximumListSize;
    private readonly int _maximumPageSize;

    #endregion Private Fields

    #region Protected Properties

    protected TContext Context { get; }
    protected IQueryable<TEntity> EntityQuery => Context.Set<TEntity>().IgnoreAutoIncludes();
    protected int MaximumBatchSize { get; }

    #endregion Protected Properties

    #region Protected Methods

    protected static Expression<Func<TEntity, bool>> HasId(TKey id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return entity => entity.Id!.Equals(id);
    }

    protected virtual IQueryable<TEntity> ApplyDetails(IQueryable<TEntity> query)
        => query;

    protected IQueryable<TEntity> WithDetails(IQueryable<TEntity> query, bool includeDetails)
        => includeDetails ? ApplyDetails(query) : query;

    #endregion Protected Methods

    #region Public Constructors

    public ReadRepository(TContext context, IOptions<RepositoryOptions> options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        Context = context;
        var limits = options.Value;
        if (!RepositoryOptions.IsValid(limits))
            throw new OptionsValidationException(nameof(RepositoryOptions), typeof(RepositoryOptions), ["Repository limits must be positive; list and batch limits must be below Int32.MaxValue."]);
        _maximumPageSize = limits.MaximumPageSize;
        _maximumListSize = limits.MaximumListSize;
        MaximumBatchSize = limits.MaximumBatchSize;
        var key = context.Model.FindEntityType(typeof(TEntity))?.FindPrimaryKey();
        if (key is null || key.Properties.Count != 1 || key.Properties[0].Name != nameof(IEntity<>.Id) ||
            key.Properties[0].IsShadowProperty() || key.Properties[0].ClrType != typeof(TKey))
            throw new NotSupportedException("Repositories require one mapped Id primary key matching TKey; composite/keyless entities need a custom query service.");
    }

    #endregion Public Constructors

    #region Public Methods

    public virtual Task<bool> AnyAsync(CancellationToken cancellationToken = default) => EntityQuery.AnyAsync(cancellationToken);

    public virtual Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default) => EntityQuery.AnyAsync(HasId(id), cancellationToken);

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        var items = await WithDetails(EntityQuery.OrderBy(e => e.Id), includeDetails).AsNoTracking()
            .Take(_maximumListSize + 1).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (items.Count > _maximumListSize) throw new InvalidOperationException("Result exceeds MaximumListSize. Use paging or a custom streaming query.");
        return items.AsReadOnly();
    }

    public virtual Task<TEntity?> GetAsync(TKey id, bool includeDetails = false, CancellationToken cancellationToken = default)
                => WithDetails(EntityQuery, includeDetails).AsNoTracking().SingleOrDefaultAsync(HasId(id), cancellationToken);

    public virtual Task<long> GetCountAsync(CancellationToken cancellationToken = default) => EntityQuery.LongCountAsync(cancellationToken);

    public virtual async Task<Page<TEntity>> GetPagedListAsync(long currentPage, int pageSize, bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, _maximumPageSize);
        var requested = new PageInfo(currentPage, pageSize, 0);
        if (requested.Offset > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(currentPage), "EF offset pagination is limited to Int32.MaxValue.");
        var total = await EntityQuery.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var items = await WithDetails(EntityQuery.OrderBy(e => e.Id), includeDetails).AsNoTracking()
            .Skip((int)requested.Offset).Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);
        // Count/items are separate observations unless the caller supplies a snapshot transaction.
        if (items.Count != 0) total = Math.Max(total, requested.Offset + items.Count);
        return new Page<TEntity>(items, new PageInfo(currentPage, pageSize, total));
    }

    #endregion Public Methods
}

public class ReadRepository<TEntity, TContext>(TContext context, IOptions<RepositoryOptions> options)
    : ReadRepository<TEntity, KeyType, TContext>(context, options), IReadRepository<TEntity>
    where TEntity : class, IEntity where TContext : DbContext, ICraftDbContext;
