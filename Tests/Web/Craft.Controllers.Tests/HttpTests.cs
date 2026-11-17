using System.Net;
using System.Net.Http.Json;
using Craft.Auditing.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Craft.Controllers.Tests;

public class HttpTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static async Task Status(HttpResponseMessage response, int status)
    {
        Assert.Equal(status, (int)response.StatusCode);
        if (status >= 400 && status is not (401 or 403))
        {
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
            Assert.DoesNotContain("secret", await response.Content.ReadAsStringAsync(Token));
        }
        response.Dispose();
    }

    [Fact]
    public async Task Crud_UsesDtoRoutesETagsAuditAndSoftDeletion()
    {
        await using var api = await Api.Start(); var row = await api.Create("first");
        using var get = await api.Send(HttpMethod.Get, $"items/items/{row.Id}?includeDetails=true");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode); Assert.Equal(row.ETag, get.Headers.ETag!.ToString());
        Assert.DoesNotContain("tenantId", await get.Content.ReadAsStringAsync(Token));
        using var update = await api.Send(HttpMethod.Put, $"items/items/{row.Id}", new { Name = "changed", TenantId = "evil", IsDeleted = true, Id = 999 }, row.ETag);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var changed = (await update.Content.ReadFromJsonAsync<Output>(Token))!; Assert.NotEqual(row.ETag, changed.ETag);
        await Status(await api.Send(HttpMethod.Put, $"items/items/{row.Id}", new Input("stale"), row.ETag), 412);
        using var delete = await api.Send(HttpMethod.Delete, $"items/items/{row.Id}", tag: changed.ETag);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode); var deletedTag = delete.Headers.ETag!.ToString();
        await Status(await api.Send(HttpMethod.Get, $"items/items/{row.Id}"), 404);
        using var deleted = await api.Send(HttpMethod.Get, $"items/items/{row.Id}/deleted");
        Assert.Equal(deletedTag, deleted.Headers.ETag!.ToString());
        await Status(await api.Send(HttpMethod.Post, $"items/items/{row.Id}/restore", tag: deletedTag), 200);
        await Status(await api.Send(HttpMethod.Get, $"items/items/{row.Id}/deleted"), 404);
        using var scope = api.Host.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<Store>();
        var saved = await db.Items.SingleAsync(Token); Assert.Equal("a", saved.TenantId); Assert.False(saved.IsDeleted); Assert.Equal(row.Id, saved.Id);
        Assert.Equal(4, await db.Audits.CountAsync(Token));
        Assert.Contains(await db.Audits.ToListAsync(Token), a => a.Operation == AuditOperation.Restored);
    }

    [Theory]
    [InlineData(null, 428)] [InlineData("bad", 400)] [InlineData("*", 400)]
    [InlineData("\"old\"", 412)] [InlineData("W/\"old\"", 412)] [InlineData("\"a\", \"b\"", 400)]
    public async Task Preconditions_RejectMissingMalformedWeakOrStaleTags(string? tag, int status)
    {
        await using var api = await Api.Start(); var row = await api.Create("original");
        await Status(await api.Send(HttpMethod.Put, $"items/items/{row.Id}", new Input("changed"), tag), status);
        await Status(await api.Send(HttpMethod.Delete, $"items/items/{row.Id}", tag: tag), status);
        await Status(await api.Send(HttpMethod.Delete, $"items/items/{row.Id}", tag: row.ETag), 204);
        await Status(await api.Send(HttpMethod.Post, $"items/items/{row.Id}/restore", tag: tag), status);
    }

    [Fact]
    public async Task AuthValidationPagingAndMissingResources_HaveHttpSemantics()
    {
        await using var api = await Api.Start();
        api.Client.DefaultRequestHeaders.Remove("Test-User"); await Status(await api.Send(HttpMethod.Get, "items"), 401);
        api.Client.DefaultRequestHeaders.Add("Test-User", "test");
        await Status(await api.Send(HttpMethod.Post, "items", new Input("")), 400);
        await Status(await api.Send(HttpMethod.Post, "items", new Input(new string('x', 21))), 400);
        await Status(await api.Send(HttpMethod.Post, "items", new Input("denied")), 403);
        await Status(await api.Send(HttpMethod.Post, "items"), 415);
        foreach (var query in new[] { "page=0", "pageSize=0", "pageSize=51", "page=9223372036854775807", "page=invalid" })
            await Status(await api.Send(HttpMethod.Get, "items?" + query), 400);
        await api.Create("one");
        foreach (var path in new[] { "items?includeDetails=true", "items/count", "items/any", "items/items/1/exists", "items/items/999/exists" })
            await Status(await api.Send(HttpMethod.Get, path), 200);
        await Status(await api.Send(HttpMethod.Put, "items/items/999", new Input("missing")), 404);
        await Status(await api.Send(HttpMethod.Delete, "items/items/999"), 404);
        await Status(await api.Send(HttpMethod.Post, "items/items/999/restore"), 404);
        api.Client.DefaultRequestHeaders.Add("Tenant", "b");
        await Status(await api.Send(HttpMethod.Get, "items/items/1"), 404);
        using var count = await api.Send(HttpMethod.Get, "items/count"); Assert.Equal("0", await count.Content.ReadAsStringAsync(Token));
    }

    [Fact]
    public async Task AuthorizationHook_CoversEveryEntityMutationAndRead()
    {
        await using var api = await Api.Start();
        using (var scope = api.Host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Store>(); db.Add(new Item { Name = "denied" }); await db.SaveChangesAsync(Token);
        }
        await Status(await api.Send(HttpMethod.Get, "items/items/1"), 403);
        await Status(await api.Send(HttpMethod.Get, "items/items/1/exists"), 403);
        await Status(await api.Send(HttpMethod.Put, "items/items/1", new Input("new")), 403);
        await Status(await api.Send(HttpMethod.Delete, "items/items/1"), 403);
        await Status(await api.Send(HttpMethod.Post, "items/batch", new[] { new Input("allowed"), new Input("denied") }), 403);
        await Status(await api.Send(HttpMethod.Post, "items/batch/delete", new[] { new EntityReference<int>(1) }), 403);
        using (var scope = api.Host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Store>(); db.Remove(await db.Items.SingleAsync(Token)); await db.SaveChangesAsync(Token);
        }
        await Status(await api.Send(HttpMethod.Get, "items/items/1/deleted"), 403);
        await Status(await api.Send(HttpMethod.Post, "items/items/1/restore"), 403);
    }

    [Fact]
    public async Task Batches_ValidateAllMembersThenCommitOnceAndRollBackFailures()
    {
        await using var api = await Api.Start();
        using var create = await api.Send(HttpMethod.Post, "items/batch", new[] { new Input("one"), new Input("two") });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode); var rows = (await create.Content.ReadFromJsonAsync<Output[]>(Token))!;
        await Status(await api.Send(HttpMethod.Put, "items/batch", new[] { new EntityUpdate<int, Input>(rows[0].Id, new("not-saved"), rows[0].ETag), new EntityUpdate<int, Input>(999, new("missing"), rows[1].ETag) }), 404);
        await Status(await api.Send(HttpMethod.Post, "items/batch/delete", rows.Select(r => new EntityReference<int>(r.Id, "\"stale\""))), 412);
        using var update = await api.Send(HttpMethod.Put, "items/batch", rows.Select(r => new EntityUpdate<int, Input>(r.Id, new(r.Name + "!"), r.ETag)));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode); rows = (await update.Content.ReadFromJsonAsync<Output[]>(Token))!;
        await Status(await api.Send(HttpMethod.Post, "items/batch/delete", rows.Select(r => new EntityReference<int>(r.Id, r.ETag))), 204);
        var deleted = new List<EntityReference<int>>();
        foreach (var row in rows)
        {
            using var result = await api.Send(HttpMethod.Get, $"items/items/{row.Id}/deleted"); deleted.Add(new(row.Id, result.Headers.ETag!.ToString()));
        }
        await Status(await api.Send(HttpMethod.Post, "items/batch/restore", new[] { new EntityReference<int>(999) }), 404);
        await Status(await api.Send(HttpMethod.Post, "items/batch/restore", deleted), 200);
        await Status(await api.Send(HttpMethod.Post, "items/batch", new[] { new Input("duplicate"), new Input("duplicate") }), 500);
        using var scope = api.Host.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<Store>();
        Assert.Equal(2, await db.Items.CountAsync(Token)); Assert.Equal(8, await db.Audits.CountAsync(Token));
    }

    [Fact]
    public async Task BatchBodies_RejectNullsDuplicatesEmptyOversizedAndInvalidModels()
    {
        await using var api = await Api.Start();
        await Status(await api.Send(HttpMethod.Post, "unrelated", new string?[] { null }), 200);
        await Status(await api.Send(HttpMethod.Post, "items/batch", Array.Empty<Input>()), 400);
        await Status(await api.Send(HttpMethod.Post, "items/batch", Enumerable.Range(0, 4).Select(i => new Input(i.ToString()))), 400);
        await Status(await api.Send(HttpMethod.Post, "items/batch", new Input?[] { null }), 400);
        await Status(await api.Send(HttpMethod.Put, "items/batch", new[] { new EntityUpdate<int, Input>(1, null!) }), 400);
        await Status(await api.Send(HttpMethod.Post, "items/batch/delete", Array.Empty<EntityReference<int>>()), 400);
        await Status(await api.Send(HttpMethod.Post, "items/batch/delete", new[] { new EntityReference<int>(1), new EntityReference<int>(1) }), 400);
        await Status(await api.Send(HttpMethod.Post, "items/batch/delete", new EntityReference<int>?[] { null }), 400);
    }

    [Fact]
    public async Task Errors_AreSanitizedAndNonConcurrencyEntitiesNeedNoPrecondition()
    {
        await using var api = await Api.Start(); var row = await api.Create("original");
        await Status(await api.Send(HttpMethod.Put, $"items/items/{row.Id}", new Input("race"), row.ETag), 412);
        using var fresh = await api.Send(HttpMethod.Get, $"items/items/{row.Id}");
        await Status(await api.Send(HttpMethod.Put, $"items/items/{row.Id}", new Input("explode"), fresh.Headers.ETag!.ToString()), 500);
        using var plain = await api.Send(HttpMethod.Post, "plain", new Input("count"));
        Assert.Equal(HttpStatusCode.Created, plain.StatusCode); Assert.Null(plain.Headers.ETag);
        using var lookup = await api.Client.GetAsync(plain.Headers.Location, Token); Assert.Equal(HttpStatusCode.OK, lookup.StatusCode);
        await Status(await api.Send(HttpMethod.Put, "plain/items/count", new Input("ignored")), 200);
        await Status(await api.Send(HttpMethod.Delete, "plain/items/count"), 204);
    }
}
