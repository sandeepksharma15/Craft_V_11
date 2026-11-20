using Craft.Core.Pagination;
using Craft.QuerySpec;
using MapsterMapper;

namespace Craft.HttpServices;

public class HttpQueryReadService<TResponse, TKey>(HttpClient client, string endpoint,
    IMapper mapper, HttpServiceOptions? options = null) : HttpReadService<TResponse, TKey>(client, endpoint, mapper, options), IHttpQueryService<TResponse>
    where TKey : notnull
{
    public virtual Task<HttpResult<Page<TResponse>>> SearchAsync(QueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return SendAsync<Page<TResponse>>(HttpMethod.Post, $"{Endpoint}/query", request, cancellationToken: cancellationToken);
    }

    public virtual Task<HttpResult<long>> QueryCountAsync(QueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return SendAsync<long>(HttpMethod.Post, $"{Endpoint}/query/count", request, cancellationToken: cancellationToken);
    }

    public virtual Task<HttpResult<bool>> QueryAnyAsync(QueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return SendAsync<bool>(HttpMethod.Post, $"{Endpoint}/query/any", request, cancellationToken: cancellationToken);
    }
}
