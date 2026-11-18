using Craft.Domain.Abstractions;
using Craft.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Craft.Controllers;

public abstract class EntityController<TEntity, TResponse, TCreate, TUpdate, TKey>(IRepository<TEntity, TKey> repository, IOptions<RepositoryOptions> options)
    : EntityReadController<TEntity, TResponse, TKey>(repository, options)
    where TEntity : class, IEntity<TKey> where TCreate : class where TUpdate : class where TKey : notnull
{
    #region Protected Properties

    protected IRepository<TEntity, TKey> Repository { get; } = repository;

    #endregion Protected Properties

    #region Protected Methods

    protected abstract void ApplyUpdate(TUpdate model, TEntity entity);

    protected ActionResult? CheckBatch(int count) => count < 1 || count > MaximumBatchSize
        ? Problem(statusCode: 400, title: "The batch size is outside the configured limits.") : null;

    protected abstract TEntity CreateEntity(TCreate model);

    protected async Task<(List<TEntity> Entities, ActionResult? Error)> LoadBatchAsync(IReadOnlyList<EntityReference<TKey>> references,
        EntityOperation operation, CancellationToken cancellationToken)
    {
        if (CheckBatch(references.Count) is { } sizeError) return ([], sizeError);
        if (references.Select(r => r.Id).Distinct().Count() != references.Count)
            return ([], Problem(statusCode: 400, title: "Batch IDs must be unique."));
        var entities = new List<TEntity>(references.Count);
        foreach (var reference in references)
        {
            var entity = operation == EntityOperation.Restore
                ? await Repository.LoadDeletedAsync(reference.Id, cancellationToken: cancellationToken)
                : await Repository.LoadForUpdateAsync(reference.Id, cancellationToken: cancellationToken);
            if (entity is null) return ([], NotFound());
            if (!await AuthorizeEntityAsync(entity, operation, cancellationToken)) return ([], Forbid());
            if (CheckPrecondition(entity, reference.IfMatch) is { } error) return ([], error);
            entities.Add(entity);
        }
        return (entities, null);
    }

    #endregion Protected Methods

    #region Public Methods

    [HttpPost]
    public virtual async Task<ActionResult<TResponse>> CreateAsync([FromBody] TCreate model, CancellationToken cancellationToken = default)
    {
        var entity = CreateEntity(model);
        if (!await AuthorizeEntityAsync(entity, EntityOperation.Create, cancellationToken)) return Forbid();
        await Repository.AddAsync(entity, cancellationToken: cancellationToken);
        SetETag(entity);
        return CreatedAtAction(nameof(GetAsync), new { id = entity.Id }, MapResponse(entity));
    }

    [HttpPost("batch")]
    public virtual async Task<ActionResult<IReadOnlyList<TResponse>>> CreateBatchAsync([FromBody] IReadOnlyList<TCreate> models, CancellationToken cancellationToken = default)
    {
        if (CheckBatch(models.Count) is { } error) return error;
        var entities = models.Select(CreateEntity).ToArray();
        foreach (var entity in entities)
            if (!await AuthorizeEntityAsync(entity, EntityOperation.Create, cancellationToken)) return Forbid();
        await Repository.AddRangeAsync(entities, cancellationToken: cancellationToken);
        return Ok(entities.Select(MapResponse).ToArray());
    }

    [HttpDelete("items/{id}")]
    public virtual async Task<IActionResult> DeleteAsync([FromRoute] TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await Repository.LoadForUpdateAsync(id, cancellationToken: cancellationToken);
        if (entity is null) return NotFound();
        if (!await AuthorizeEntityAsync(entity, EntityOperation.Delete, cancellationToken)) return Forbid();
        if (CheckPrecondition(entity, Request.Headers.IfMatch.ToString()) is { } error) return error;
        await Repository.DeleteAsync(entity, cancellationToken: cancellationToken);
        SetETag(entity);
        return NoContent();
    }

    [HttpPost("batch/delete")]
    public virtual async Task<IActionResult> DeleteBatchAsync([FromBody] IReadOnlyList<EntityReference<TKey>> references, CancellationToken cancellationToken = default)
    {
        var (entities, error) = await LoadBatchAsync(references, EntityOperation.Delete, cancellationToken);
        if (error is not null) return error;
        await Repository.DeleteRangeAsync(entities, cancellationToken: cancellationToken);
        return NoContent();
    }

    [HttpPut("items/{id}")]
    public virtual async Task<ActionResult<TResponse>> UpdateAsync([FromRoute] TKey id, [FromBody] TUpdate model, CancellationToken cancellationToken = default)
    {
        var entity = await Repository.LoadForUpdateAsync(id, cancellationToken: cancellationToken);
        if (entity is null) return NotFound();
        if (!await AuthorizeEntityAsync(entity, EntityOperation.Update, cancellationToken)) return Forbid();
        if (CheckPrecondition(entity, Request.Headers.IfMatch.ToString()) is { } error) return error;
        ApplyUpdate(model, entity);
        await Repository.UpdateAsync(entity, cancellationToken: cancellationToken);
        SetETag(entity);
        return Ok(MapResponse(entity));
    }

    [HttpPut("batch")]
    public virtual async Task<ActionResult<IReadOnlyList<TResponse>>> UpdateBatchAsync([FromBody] IReadOnlyList<EntityUpdate<TKey, TUpdate>> models, CancellationToken cancellationToken = default)
    {
        var (entities, error) = await LoadBatchAsync([.. models.Select(m => new EntityReference<TKey>(m.Id, m.IfMatch))], EntityOperation.Update, cancellationToken);
        if (error is not null) return error;
        for (var i = 0; i < entities.Count; i++) ApplyUpdate(models[i].Model, entities[i]);
        await Repository.UpdateRangeAsync(entities, cancellationToken: cancellationToken);
        return Ok(entities.Select(MapResponse).ToArray());
    }

    #endregion Public Methods
}

public abstract class EntityController<TEntity, TResponse, TCreate, TUpdate>(IRepository<TEntity> repository, IOptions<RepositoryOptions> options)
    : EntityController<TEntity, TResponse, TCreate, TUpdate, KeyType>(repository, options)
    where TEntity : class, IEntity where TCreate : class where TUpdate : class;
