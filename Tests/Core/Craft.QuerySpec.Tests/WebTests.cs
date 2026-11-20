using System.Linq.Expressions;
using System.Net;
using System.Text.Json;
using Craft.Controllers;
using Craft.Controllers.Tests;
using Craft.Core.Pagination;
using Craft.HttpServices;
using Craft.Repositories;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Craft.QuerySpec.Tests;

public sealed class WebTests
{
    public static QueryPolicy<Item> Policy() => new QueryPolicy<Item>().Allow("name", item => item.Name, true, FilterOperator.Contains, FilterOperator.Equal);

    [Fact]
    public async Task AllControllerAndClientVariants_UseTheSameBoundedQueryContract()
    {
        await using Api api = await Api.Start(configure: services => services.AddScoped<IQueryReadRepository<Item, int>, QueryRepository<Item, int, Store>>());
        await api.Create("alpha");
        await api.Create("beta");
        IMapper mapper = new Mapper(new TypeAdapterConfig());
        IHttpQueryService<Output>[] clients =
        [
            new HttpQueryReadService<Output, int>(api.Client, "api/queryread", mapper),
            new HttpQueryService<Output, Input, Input, int>(api.Client, "api/querycrud", mapper),
            new HttpQuerySoftDeleteService<Output, Input, Input, int>(api.Client, "api/querysoft", mapper)
        ];
        CancellationToken token = TestContext.Current.CancellationToken;
        foreach (IHttpQueryService<Output> client in clients)
        {
            QueryRequest request = new()
            {
                Filter = new FilterGroup { Filters = [new("name", FilterOperator.Equal, JsonSerializer.SerializeToElement("alpha"))] },
                Sort = [new("name", true)],
                PageSize = 1
            };
            HttpResult<Page<Output>> page = await client.SearchAsync(request, token);
            Assert.Equal("alpha", Assert.Single(page.Outcome.Value.Items).Name);
            Assert.Equal(1, (await client.QueryCountAsync(request, token)).Outcome.Value);
            Assert.True((await client.QueryAnyAsync(request, token)).Outcome.Value);
            QueryRequest invalid = new() { Sort = [new("TenantId")] };
            Assert.Equal(HttpStatusCode.BadRequest, (await client.SearchAsync(invalid, token)).Response.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.QueryCountAsync(invalid, token)).Response.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.QueryAnyAsync(invalid, token)).Response.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.SearchAsync(new QueryRequest { PageSize = 51 }, token)).Response.StatusCode);
            await Assert.ThrowsAsync<ArgumentNullException>(() => client.SearchAsync(null!, token));
            await Assert.ThrowsAsync<ArgumentNullException>(() => client.QueryCountAsync(null!, token));
            await Assert.ThrowsAsync<ArgumentNullException>(() => client.QueryAnyAsync(null!, token));
        }
        using HttpResponseMessage bypass = await api.Send(HttpMethod.Post, "queryread/query", new { IgnoreQueryFilters = true });
        Assert.Equal(HttpStatusCode.BadRequest, bypass.StatusCode);
        using HttpResponseMessage delete = await api.Send(HttpMethod.Post, "querycrud/deletebyquery", new QueryRequest());
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        api.Client.DefaultRequestHeaders.Remove("Test-User");
        Assert.Equal(HttpStatusCode.Unauthorized, (await clients[0].SearchAsync(new QueryRequest(), token)).Response.StatusCode);
    }
}

public sealed class QueryReadController(IQueryReadRepository<Item, int> queries, IOptions<RepositoryOptions> options, IMapper mapper)
    : QueryReadController<Item, Output, int>(queries, options, mapper)
{
    protected override QueryPolicy<Item> QueryPolicy => WebTests.Policy();
    protected override Expression<Func<Item, Output>> Projection => item => new Output(item.Id, item.Name, null);
}

public sealed class QueryCrudController(IQueryReadRepository<Item, int> queries, IRepository<Item, int> repository,
    IOptions<RepositoryOptions> options, IMapper mapper)
    : QueryEntityController<Item, Output, Input, Input, int>(queries, repository, options, mapper)
{
    protected override QueryPolicy<Item> QueryPolicy => WebTests.Policy();
    protected override Expression<Func<Item, Output>> Projection => item => new Output(item.Id, item.Name, null);
    protected override void ApplyUpdate(Input model, Item entity) => entity.Name = model.Name;
}

public sealed class QuerySoftController(IQueryReadRepository<Item, int> queries, IRepository<Item, int> repository,
    IOptions<RepositoryOptions> options, IMapper mapper)
    : QuerySoftDeleteEntityController<Item, Output, Input, Input, int>(queries, repository, options, mapper)
{
    protected override QueryPolicy<Item> QueryPolicy => WebTests.Policy();
    protected override Expression<Func<Item, Output>> Projection => item => new Output(item.Id, item.Name, null);
    protected override void ApplyUpdate(Input model, Item entity) => entity.Name = model.Name;
}
