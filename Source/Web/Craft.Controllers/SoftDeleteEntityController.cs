using Craft.Domain.Abstractions;
using Craft.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Craft.Controllers;

public abstract class SoftDeleteEntityController<TEntity, TResponse, TCreate, TUpdate, TKey>(IRepository<TEntity, TKey> repository, IOptions<RepositoryOptions> options)
    : EntityController<TEntity, TResponse, TCreate, TUpdate, TKey>(repository, options)
    where TEntity : class, IEntity<TKey>, ISoftDelete where TCreate : class where TUpdate : class where TKey : notnull
{
    [HttpGet("items/{id}/deleted")]
    public virtual async Task<ActionResult<TResponse>> GetDeletedAsync([FromRoute] TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await Repository.LoadDeletedAsync(id, cancellationToken: cancellationToken);
        if (entity is null) return NotFound();
        if (!await AuthorizeEntityAsync(entity, EntityOperation.Restore, cancellationToken)) return Forbid();
        SetETag(entity);
        return Ok(MapResponse(entity));
    }

    [HttpPost("items/{id}/restore")]
    public virtual async Task<ActionResult<TResponse>> RestoreAsync([FromRoute] TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await Repository.LoadDeletedAsync(id, cancellationToken: cancellationToken);
        if (entity is null) return NotFound();
        if (!await AuthorizeEntityAsync(entity, EntityOperation.Restore, cancellationToken)) return Forbid();
        if (CheckPrecondition(entity, Request.Headers.IfMatch.ToString()) is { } error) return error;
        await Repository.RestoreAsync(entity, cancellationToken: cancellationToken);
        SetETag(entity);
        return Ok(MapResponse(entity));
    }

    [HttpPost("batch/restore")]
    public virtual async Task<ActionResult<IReadOnlyList<TResponse>>> RestoreBatchAsync([FromBody] IReadOnlyList<EntityReference<TKey>> references, CancellationToken cancellationToken = default)
    {
        var (entities, error) = await LoadBatchAsync(references, EntityOperation.Restore, cancellationToken);
        if (error is not null) return error;
        await Repository.RestoreRangeAsync(entities, cancellationToken: cancellationToken);
        return Ok(entities.Select(MapResponse).ToArray());
    }
}

public abstract class SoftDeleteEntityController<TEntity, TResponse, TCreate, TUpdate>(IRepository<TEntity> repository, IOptions<RepositoryOptions> options)
    : SoftDeleteEntityController<TEntity, TResponse, TCreate, TUpdate, KeyType>(repository, options)
    where TEntity : class, IEntity, ISoftDelete where TCreate : class where TUpdate : class;
