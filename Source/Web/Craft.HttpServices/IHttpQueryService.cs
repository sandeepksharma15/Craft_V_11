using Craft.Core.Pagination;
using Craft.QuerySpec;

namespace Craft.HttpServices;

public interface IHttpQueryService<TResponse>
{
    Task<HttpResult<Page<TResponse>>> SearchAsync(QueryRequest request, CancellationToken cancellationToken = default);
    Task<HttpResult<long>> QueryCountAsync(QueryRequest request, CancellationToken cancellationToken = default);
    Task<HttpResult<bool>> QueryAnyAsync(QueryRequest request, CancellationToken cancellationToken = default);
}
