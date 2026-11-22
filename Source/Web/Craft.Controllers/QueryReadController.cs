using System.Linq.Expressions;
using Craft.Core.Pagination;
using Craft.Domain.Abstractions;
using Craft.QuerySpec;
using Craft.Repositories;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Craft.Controllers;

public abstract class QueryReadController<TEntity, TResponse, TKey> : EntityReadController<TEntity, TResponse, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : notnull
{
    private readonly int _maximumPageSize;
    private readonly IQueryReadRepository<TEntity, TKey> _queries;

    protected QueryReadController(IQueryReadRepository<TEntity, TKey> queryRepository,
        IOptions<RepositoryOptions> options, IMapper mapper) : base(queryRepository, options, mapper)
    {
        ArgumentNullException.ThrowIfNull(queryRepository);
        _queries = queryRepository;
        _maximumPageSize = options.Value.MaximumPageSize;
    }

    protected abstract QueryPolicy<TEntity> QueryPolicy { get; }

    protected abstract Expression<Func<TEntity, TResponse>> Projection { get; }

    [HttpPost("query")]
    public virtual Task<ActionResult<Page<TResponse>>> SearchAsync([FromBody] QueryRequest request,
        CancellationToken cancellationToken = default)
        => QueryEndpoints.SearchAsync(this, _queries, QueryPolicy, Projection, request, _maximumPageSize, cancellationToken);

    [HttpPost("query/count")]
    public virtual Task<ActionResult<long>> QueryCountAsync([FromBody] QueryRequest request,
        CancellationToken cancellationToken = default)
        => QueryEndpoints.CountAsync(this, _queries, QueryPolicy, request, cancellationToken);

    [HttpPost("query/any")]
    public virtual Task<ActionResult<bool>> QueryAnyAsync([FromBody] QueryRequest request,
        CancellationToken cancellationToken = default)
        => QueryEndpoints.AnyAsync(this, _queries, QueryPolicy, request, cancellationToken);
}
