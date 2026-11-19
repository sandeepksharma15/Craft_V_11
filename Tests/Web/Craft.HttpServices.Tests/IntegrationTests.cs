using System.Net;
using Craft.Controllers.Tests;
using Craft.Core.Pagination;
using Craft.Core.Results;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Craft.HttpServices.Tests;

public sealed class IntegrationTests
{
    [Fact]
    public async Task Client_Controller_Repository_Context_RoundTripPreservesConcurrencyAndAudit()
    {
        await using Api api = await Api.Start();
        HttpSoftDeleteService<Output, Input, Input, int> service = new(api.Client, "api/items", new Mapper(new TypeAdapterConfig()));
        CancellationToken token = TestContext.Current.CancellationToken;

        HttpResult<Output> created = await service.CreateAsync(new Input("first")
        {
            Id = 999,
            TenantId = "untrusted",
            ConcurrencyStamp = "untrusted",
            IsDeleted = true
        }, token);
        Assert.True(created.Outcome.IsSuccess);
        Assert.Equal(HttpStatusCode.Created, created.Response.StatusCode);
        Assert.NotNull(created.Response.Location);
        Assert.Equal(created.Outcome.Value.ETag, created.Response.ETag);
        int id = created.Outcome.Value.Id;

        HttpResult<Output> read = await service.GetAsync(id, true, token);
        Assert.Equal(created.Outcome.Value, read.Outcome.Value);
        Assert.Equal(1, (await service.CountAsync(token)).Outcome.Value);
        Assert.True((await service.AnyAsync(token)).Outcome.Value);
        Assert.True((await service.ExistsAsync(id, token)).Outcome.Value);
        Assert.False((await service.ExistsAsync(int.MaxValue, token)).Outcome.Value);
        HttpResult<Page<Output>> page = await service.GetPageAsync(cancellationToken: token);
        Assert.Single(page.Outcome.Value.Items);
        Assert.Equal(20, page.Outcome.Value.Pagination.PageSize);
        Assert.Single((await service.GetPageAsync(1, 1, true, token)).Outcome.Value.Items);

        Assert.Equal((HttpStatusCode)428, (await service.UpdateAsync(id, new Input("next"), cancellationToken: token)).Response.StatusCode);
        HttpResult<Output> updated = await service.UpdateAsync(id, new Input("next")
        {
            Id = 999,
            TenantId = "untrusted",
            ConcurrencyStamp = "untrusted",
            IsDeleted = true
        }, read.Response.ETag, token);
        Assert.Equal("next", updated.Outcome.Value.Name);
        Assert.NotEqual(read.Response.ETag, updated.Response.ETag);
        Assert.Equal(HttpStatusCode.PreconditionFailed, (await service.DeleteAsync(id, read.Response.ETag, token)).Response.StatusCode);
        HttpResult deleted = await service.DeleteAsync(id, updated.Response.ETag, token);
        Assert.True(deleted.Outcome.IsSuccess);
        Assert.Equal(HttpStatusCode.NoContent, deleted.Response.StatusCode);
        Assert.Equal(ErrorKind.NotFound, Assert.Single((await service.GetAsync(id, cancellationToken: token)).Outcome.Errors).Kind);
        HttpResult<Output> tombstone = await service.GetDeletedAsync(id, token);
        Assert.Equal(deleted.Response.ETag, tombstone.Response.ETag);
        HttpResult<Output> restored = await service.RestoreAsync(id, tombstone.Response.ETag, token);
        Assert.True(restored.Outcome.IsSuccess);
        Assert.NotEqual(tombstone.Response.ETag, restored.Response.ETag);

        using IServiceScope scope = api.Host.Services.CreateScope();
        Store store = scope.ServiceProvider.GetRequiredService<Store>();
        Assert.Equal(4, await store.Audits.CountAsync(token));
        Item entity = await store.Items.SingleAsync(token);
        Assert.Equal("a", entity.TenantId);
        Assert.Equal(id, entity.Id);
        Assert.NotEqual(999, entity.Id);
        Assert.False(entity.IsDeleted);
        Assert.NotEqual("untrusted", entity.ConcurrencyStamp);
    }

    [Fact]
    public async Task Batches_UsePerItemTokensAndCommitAtomically()
    {
        await using Api api = await Api.Start();
        HttpSoftDeleteService<Output, Input, Input, int> service = new(api.Client, "api/items", new Mapper(new TypeAdapterConfig()));
        CancellationToken token = TestContext.Current.CancellationToken;

        HttpResult<IReadOnlyList<Output>> created = await service.CreateBatchAsync([new("one"), new("two")], token);
        Assert.Equal(2, created.Outcome.Value.Count);
        EntityUpdate<int, Input>[] edits = [.. created.Outcome.Value.Select(item =>
            new EntityUpdate<int, Input>(item.Id, new Input(item.Name + " edited"), item.ETag))];
        HttpResult<IReadOnlyList<Output>> updated = await service.UpdateBatchAsync(edits, token);
        Assert.All(updated.Outcome.Value, item => Assert.EndsWith(" edited", item.Name));

        EntityReference<int>[] references = [.. updated.Outcome.Value.Select(item => new EntityReference<int>(item.Id, item.ETag))];
        Assert.True((await service.DeleteBatchAsync(references, token)).Outcome.IsSuccess);
        List<EntityReference<int>> deleted = [];
        foreach (Output item in updated.Outcome.Value)
        {
            HttpResult<Output> result = await service.GetDeletedAsync(item.Id, token);
            deleted.Add(new EntityReference<int>(item.Id, result.Response.ETag));
        }
        Assert.Equal(2, (await service.RestoreBatchAsync(deleted, token)).Outcome.Value.Count);
        Assert.Equal(HttpStatusCode.PreconditionFailed, (await service.UpdateBatchAsync(edits, token)).Response.StatusCode);
        Assert.Equal(2, (await service.CountAsync(token)).Outcome.Value);
    }

    [Fact]
    public async Task Failures_PreserveValidationAndDoNotRetryWrites()
    {
        await using Api api = await Api.Start();
        HttpCrudService<Output, Input, Input, int> service = new(api.Client, "api/items", new Mapper(new TypeAdapterConfig()));
        CancellationToken token = TestContext.Current.CancellationToken;

        HttpResult<Output> invalid = await service.CreateAsync(new Input(""), token);
        Assert.Equal(ErrorKind.Validation, Assert.Single(invalid.Outcome.Errors).Kind);
        Assert.NotNull(invalid.Response.Problem?.Errors);
        HttpResult<Output> created = await service.CreateAsync(new Input("first"), token);
        HttpResult<Output> race = await service.UpdateAsync(created.Outcome.Value.Id, new Input("race"), created.Response.ETag, token);
        Assert.Equal(HttpStatusCode.PreconditionFailed, race.Response.StatusCode);
        Assert.Equal("first", (await service.GetAsync(created.Outcome.Value.Id, cancellationToken: token)).Outcome.Value.Name);
        api.Client.DefaultRequestHeaders.Remove("Test-User");
        Assert.Equal(ErrorKind.Unauthorized, Assert.Single((await service.CountAsync(token)).Outcome.Errors).Kind);
    }

    [Fact]
    public async Task InheritedService_MapsConsumerModelWithInjectedConfiguration()
    {
        await using Api api = await Api.Start();
        TypeAdapterConfig config = new();
        config.NewConfig<Editor, Input>().Map(destination => destination.Name, source => source.DisplayName);
        config.NewConfig<Output, Editor>().Map(destination => destination.DisplayName, source => source.Name);
        EditorService service = new(api.Client, new Mapper(config));

        HttpResult<Editor> result = await service.SaveAsync(new Editor("mapped"), TestContext.Current.CancellationToken);

        Assert.Equal("mapped", result.Outcome.Value.DisplayName);
        Assert.Equal(HttpStatusCode.Created, result.Response.StatusCode);
        Assert.NotNull(result.Response.ETag);
    }

    public sealed record Editor(string DisplayName);

    private sealed class EditorService(HttpClient client, IMapper mapper)
        : HttpCrudService<Output, Input, Input, int>(client, "api/items", mapper)
    {
        public async Task<HttpResult<Editor>> SaveAsync(Editor model, CancellationToken cancellationToken)
        {
            Input request = Mapper.Map<Editor, Input>(model);
            HttpResult<Output> response = await CreateAsync(request, cancellationToken);

            return response.Map(value => Mapper.Map<Output, Editor>(value));
        }
    }
}
