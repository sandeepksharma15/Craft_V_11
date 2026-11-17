using Craft.Core.Pagination;
using Craft.Domain.Abstractions;
using Craft.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Craft.Controllers;

[ApiController]
[TypeFilter(typeof(BatchBodyValidationFilter))]
[Authorize]
[Route("api/[controller]")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public abstract class EntityReadController<TEntity, TResponse, TKey> : ControllerBase
    where TEntity : class, IEntity<TKey> where TKey : notnull
{
    protected IReadRepository<TEntity, TKey> ReadRepository { get; }
    protected int MaximumBatchSize { get; }
    private readonly int _maximumPageSize;

    protected EntityReadController(IReadRepository<TEntity, TKey> repository, IOptions<RepositoryOptions> options)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(options);
        ReadRepository = repository;
        _maximumPageSize = options.Value.MaximumPageSize;
        MaximumBatchSize = options.Value.MaximumBatchSize;
        if (_maximumPageSize < 1 || MaximumBatchSize < 1)
            throw new ArgumentException("Controller limits must be positive.", nameof(options));
    }

    protected abstract TResponse MapResponse(TEntity entity);
    protected virtual ValueTask<bool> AuthorizeEntityAsync(TEntity entity, EntityOperation operation, CancellationToken cancellationToken)
        => ValueTask.FromResult(true);

    protected void SetETag(TEntity entity)
    {
        var tag = ConcurrencyTags.Get(entity);
        if (tag is not null) Response.Headers.ETag = tag;
    }

    protected ActionResult? CheckPrecondition(TEntity entity, string? supplied)
        => ConcurrencyTags.Validate(entity, supplied) is { } status
            ? Problem(statusCode: status, title: status == 428 ? "An original entity ETag is required." : "The entity precondition was not satisfied.") : null;

    [ActionName(nameof(GetAsync))]
    [HttpGet("items/{id}")]
    public virtual async Task<ActionResult<TResponse>> GetAsync([FromRoute] TKey id, [FromQuery] bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        var entity = await ReadRepository.GetAsync(id, includeDetails, cancellationToken);
        if (entity is null) return NotFound();
        if (!await AuthorizeEntityAsync(entity, EntityOperation.Read, cancellationToken)) return Forbid();
        SetETag(entity);
        return Ok(MapResponse(entity));
    }

    [HttpGet]
    public virtual async Task<ActionResult<Page<TResponse>>> GetPageAsync([FromQuery] long page = 1, [FromQuery] int? pageSize = null,
        [FromQuery] bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        var size = pageSize ?? Math.Min(20, _maximumPageSize);
        if (page < 1 || size < 1 || size > _maximumPageSize || page - 1 > int.MaxValue / size)
            return Problem(statusCode: 400, title: "The requested page is outside the configured limits.");
        var result = await ReadRepository.GetPagedListAsync(page, size, includeDetails, cancellationToken);
        return Ok(new Page<TResponse>(result.Items.Select(MapResponse).ToArray(), result.Pagination));
    }

    [HttpGet("count")]
    public virtual async Task<ActionResult<long>> CountAsync(CancellationToken cancellationToken = default)
        => Ok(await ReadRepository.GetCountAsync(cancellationToken));

    [HttpGet("items/{id}/exists")]
    public virtual async Task<ActionResult<bool>> ExistsAsync([FromRoute] TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await ReadRepository.GetAsync(id, cancellationToken: cancellationToken);
        if (entity is null) return Ok(false);
        if (!await AuthorizeEntityAsync(entity, EntityOperation.Read, cancellationToken)) return Forbid();
        return Ok(true);
    }

    [HttpGet("any")]
    public virtual async Task<ActionResult<bool>> AnyAsync(CancellationToken cancellationToken = default)
        => Ok(await ReadRepository.AnyAsync(cancellationToken));
}

public abstract class EntityReadController<TEntity, TResponse>(IReadRepository<TEntity> repository, IOptions<RepositoryOptions> options)
    : EntityReadController<TEntity, TResponse, KeyType>(repository, options) where TEntity : class, IEntity;
