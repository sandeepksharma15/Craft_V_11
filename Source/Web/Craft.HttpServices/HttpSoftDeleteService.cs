using MapsterMapper;

namespace Craft.HttpServices;

public class HttpSoftDeleteService<TResponse, TCreate, TUpdate, TKey>(HttpClient client, string endpoint,
    IMapper mapper, HttpServiceOptions? options = null)
    : HttpCrudService<TResponse, TCreate, TUpdate, TKey>(client, endpoint, mapper, options)
    where TCreate : class
    where TUpdate : class
    where TKey : notnull
{
    public virtual Task<HttpResult<TResponse>> GetDeletedAsync(TKey id, CancellationToken cancellationToken = default)
        => SendAsync<TResponse>(HttpMethod.Get, $"{ItemPath(id)}/deleted", cancellationToken: cancellationToken);

    public virtual Task<HttpResult<TResponse>> RestoreAsync(TKey id, string? ifMatch = null, CancellationToken cancellationToken = default)
        => SendAsync<TResponse>(HttpMethod.Post, $"{ItemPath(id)}/restore", ifMatch: ifMatch, cancellationToken: cancellationToken);

    public virtual Task<HttpResult<IReadOnlyList<TResponse>>> RestoreBatchAsync(IReadOnlyList<EntityReference<TKey>> references,
        CancellationToken cancellationToken = default)
    {
        ValidateBatch(references);

        return SendAsync<IReadOnlyList<TResponse>>(HttpMethod.Post, $"{Endpoint}/batch/restore", references, cancellationToken: cancellationToken);
    }
}

public class HttpSoftDeleteService<TResponse, TCreate, TUpdate>(HttpClient client, string endpoint,
    IMapper mapper, HttpServiceOptions? options = null)
    : HttpSoftDeleteService<TResponse, TCreate, TUpdate, KeyType>(client, endpoint, mapper, options)
    where TCreate : class
    where TUpdate : class;
