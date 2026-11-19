using System.Globalization;
using Craft.Core.Pagination;
using MapsterMapper;

namespace Craft.HttpServices;

public class HttpReadService<TResponse, TKey> : HttpServiceBase where TKey : notnull
{
    #region Constructors

    public HttpReadService(HttpClient client, string endpoint, IMapper mapper, HttpServiceOptions? options = null)
        : base(client, options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentNullException.ThrowIfNull(mapper);
        if (endpoint.Split('/').Any(segment => string.IsNullOrWhiteSpace(segment)
            || segment is "." or ".." || segment.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_')))
            throw new ArgumentException("Use relative endpoint segments containing letters, numbers, hyphens or underscores.", nameof(endpoint));

        Endpoint = endpoint;
        Mapper = mapper;
    }

    #endregion

    #region Properties

    protected string Endpoint { get; }

    protected IMapper Mapper { get; }

    #endregion

    #region Methods

    public virtual Task<HttpResult<TResponse>> GetAsync(TKey id, bool includeDetails = false,
        CancellationToken cancellationToken = default)
        => SendAsync<TResponse>(HttpMethod.Get, $"{ItemPath(id)}?includeDetails={includeDetails.ToString().ToLowerInvariant()}",
            cancellationToken: cancellationToken);

    public virtual Task<HttpResult<Page<TResponse>>> GetPageAsync(long page = 1, int? pageSize = null,
        bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        if (pageSize.HasValue)
            ArgumentOutOfRangeException.ThrowIfLessThan(pageSize.Value, 1);

        string path = FormattableString.Invariant($"{Endpoint}?page={page}&includeDetails={includeDetails.ToString().ToLowerInvariant()}");
        if (pageSize.HasValue)
            path += FormattableString.Invariant($"&pageSize={pageSize.Value}");

        return SendAsync<Page<TResponse>>(HttpMethod.Get, path, cancellationToken: cancellationToken);
    }

    public virtual Task<HttpResult<long>> CountAsync(CancellationToken cancellationToken = default)
        => SendAsync<long>(HttpMethod.Get, $"{Endpoint}/count", cancellationToken: cancellationToken);

    public virtual Task<HttpResult<bool>> AnyAsync(CancellationToken cancellationToken = default)
        => SendAsync<bool>(HttpMethod.Get, $"{Endpoint}/any", cancellationToken: cancellationToken);

    public virtual Task<HttpResult<bool>> ExistsAsync(TKey id, CancellationToken cancellationToken = default)
        => SendAsync<bool>(HttpMethod.Get, $"{ItemPath(id)}/exists", cancellationToken: cancellationToken);

    protected virtual string FormatKey(TKey id)
        => Convert.ToString(id, CultureInfo.InvariantCulture) ?? string.Empty;

    protected string ItemPath(TKey id)
    {
        ArgumentNullException.ThrowIfNull(id);
        string key = FormatKey(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (key is "." or ".." || key.IndexOfAny(['/', '\\']) >= 0)
            throw new ArgumentException("The key must fit a single route segment.", nameof(id));

        return $"{Endpoint}/items/{Uri.EscapeDataString(key)}";
    }

    #endregion
}

public class HttpReadService<TResponse>(HttpClient client, string endpoint, IMapper mapper, HttpServiceOptions? options = null)
    : HttpReadService<TResponse, KeyType>(client, endpoint, mapper, options);
