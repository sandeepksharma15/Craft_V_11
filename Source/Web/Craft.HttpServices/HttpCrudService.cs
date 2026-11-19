using MapsterMapper;

namespace Craft.HttpServices;

public class HttpCrudService<TResponse, TCreate, TUpdate, TKey>(HttpClient client, string endpoint,
    IMapper mapper, HttpServiceOptions? options = null)
    : HttpReadService<TResponse, TKey>(client, endpoint, mapper, options)
    where TCreate : class
    where TUpdate : class
    where TKey : notnull
{
    public virtual Task<HttpResult<TResponse>> CreateAsync(TCreate model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        return SendAsync<TResponse>(HttpMethod.Post, Endpoint, model, cancellationToken: cancellationToken);
    }

    public virtual Task<HttpResult<TResponse>> UpdateAsync(TKey id, TUpdate model, string? ifMatch = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        return SendAsync<TResponse>(HttpMethod.Put, ItemPath(id), model, ifMatch, cancellationToken);
    }

    public virtual Task<HttpResult> DeleteAsync(TKey id, string? ifMatch = null, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Delete, ItemPath(id), ifMatch: ifMatch, cancellationToken: cancellationToken);

    public virtual Task<HttpResult<IReadOnlyList<TResponse>>> CreateBatchAsync(IReadOnlyList<TCreate> models,
        CancellationToken cancellationToken = default)
    {
        ValidateBatch(models);

        return SendAsync<IReadOnlyList<TResponse>>(HttpMethod.Post, $"{Endpoint}/batch", models, cancellationToken: cancellationToken);
    }

    public virtual Task<HttpResult<IReadOnlyList<TResponse>>> UpdateBatchAsync(IReadOnlyList<EntityUpdate<TKey, TUpdate>> models,
        CancellationToken cancellationToken = default)
    {
        ValidateBatch(models);

        return SendAsync<IReadOnlyList<TResponse>>(HttpMethod.Put, $"{Endpoint}/batch", models, cancellationToken: cancellationToken);
    }

    public virtual Task<HttpResult> DeleteBatchAsync(IReadOnlyList<EntityReference<TKey>> references,
        CancellationToken cancellationToken = default)
    {
        ValidateBatch(references);

        return SendAsync(HttpMethod.Post, $"{Endpoint}/batch/delete", references, cancellationToken: cancellationToken);
    }

    protected static void ValidateBatch<T>(IReadOnlyList<T> models)
    {
        ArgumentNullException.ThrowIfNull(models);
        if (models.Count == 0 || models.Any(model => model is null))
            throw new ArgumentException("Provide a non-empty batch without null items.", nameof(models));
    }
}

public class HttpCrudService<TResponse, TCreate, TUpdate>(HttpClient client, string endpoint,
    IMapper mapper, HttpServiceOptions? options = null)
    : HttpCrudService<TResponse, TCreate, TUpdate, KeyType>(client, endpoint, mapper, options)
    where TCreate : class
    where TUpdate : class;
