using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Craft.Auditing.Abstractions;
using Craft.Auditing.Attributes;
using Craft.Auditing.Capture;
using Craft.Auditing.Configuration;
using Craft.Auditing.Model;
using Craft.Auditing.Persistence;
using Craft.Core.Abstractions;
using Craft.Data;
using Craft.Domain.Abstractions;
using Craft.Repositories;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Craft.Controllers.Tests;

[Audit]
public sealed class Item : IEntity<int>, ISoftDelete, IHasConcurrency, IHasTenant<string>
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string TenantId { get; set; } = null!;
    public string? ConcurrencyStamp { get; set; }
    public bool IsDeleted { get; set; }
}
public sealed record Input([Required, MaxLength(20)] string Name);
public sealed record Output(int Id, string Name, string? ETag);
public sealed class ItemsController(IRepository<Item, int> repository, IOptions<RepositoryOptions> options, IMapper mapper)
    : SoftDeleteEntityController<Item, Output, Input, Input, int>(repository, options, mapper)
{
    protected override void ApplyUpdate(Input model, Item entity) => Mapper.Map(model, entity);
    protected override ValueTask<bool> AuthorizeEntityAsync(Item entity, EntityOperation operation, CancellationToken cancellationToken)
        => ValueTask.FromResult(entity.Name != "denied");
}
public sealed class Plain : IEntity<string> { public string Id { get; set; } = ""; }
public sealed class PlainController(IRepository<Plain, string> repository, IOptions<RepositoryOptions> options, IMapper mapper)
    : EntityController<Plain, string, Input, Input, string>(repository, options, mapper)
{
    protected override Plain CreateEntity(Input model) => new() { Id = model.Name };
    protected override void ApplyUpdate(Input model, Plain entity) { }
    protected override string MapResponse(Plain entity) => entity.Id;
}
public sealed class FaultRepository(Store db, IOptions<RepositoryOptions> options) : Repository<Item, int, Store>(db, options)
{
    public override async Task<Item> UpdateAsync(Item entity, bool autoSave = true, CancellationToken cancellationToken = default)
    {
        if (entity.Name == "explode") throw new InvalidOperationException("secret database details");
        if (entity.Name == "race")
            await Context.Database.ExecuteSqlAsync($"UPDATE Items SET ConcurrencyStamp = {"competing-writer"} WHERE Id = {entity.Id}", cancellationToken);
        return await base.UpdateAsync(entity, autoSave, cancellationToken);
    }

}
public sealed class Store(DbContextOptions<Store> options) : CraftDbContext(options)
{
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Plain> Plain => Set<Plain>();
    public DbSet<AuditTrail> Audits => Set<AuditTrail>();
    protected override void ConfigureModel(ModelBuilder model) => model.Entity<Item>().HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
}
public sealed class Tenant(IHttpContextAccessor accessor) : ICurrentTenant<string>
{
    public string? Identifier => null;
    public bool IsAvailable => true;
    public bool TryGetId(out string id) { id = accessor.HttpContext?.Request.Headers["Tenant"].FirstOrDefault() ?? "a"; return true; }
}
public sealed class Actor : IAuditContext { public AuditIdentity GetIdentity() => new("test", "a", "request"); }
public sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(Request.Headers.ContainsKey("Test-User")
        ? AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")], Scheme.Name)), Scheme.Name))
        : AuthenticateResult.NoResult());
}
public sealed class Api : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    public IHost Host { get; }
    public HttpClient Client { get; }
    private Api(IHost host, SqliteConnection connection)
    {
        Host = host; _connection = connection; Client = host.GetTestClient(); Client.DefaultRequestHeaders.Add("Test-User", "test");
    }
    public static async Task<Api> Start(int maximumPageSize = 50)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var host = await new HostBuilder().ConfigureWebHost(web => web.UseTestServer().ConfigureServices(services =>
        {
            services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning).AddConsole());
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentTenant<string>, Tenant>();
            services.AddAuthentication("test").AddScheme<AuthenticationSchemeOptions, TestAuthentication>("test", _ => { });
            services.AddAuthorization();
            services.AddCraftDbContext<Store>(o => o.UseSqlite(connection), f => f.UseSoftDelete().UseConcurrency().UseMultiTenancy<string>()
                .AddFeature(_ => new AuditingFeature(new AuditTrailFactory(new Actor(), Options.Create(new AuditOptions()), TimeProvider.System))));
            services.AddCraftRepositories<Store>(r => r.AddCustom<Item, int, FaultRepository>().Add<Plain, string>(), o => { o.MaximumPageSize = maximumPageSize; o.MaximumBatchSize = 3; });
            TypeAdapterConfig mapping = new();
            mapping.NewConfig<Input, Item>().IgnoreNonMapped(true).Map(destination => destination.Name, source => source.Name);
            mapping.NewConfig<Item, Output>().Map(destination => destination.ETag, source => ConcurrencyTags.Get(source));
            services.AddSingleton(mapping);
            services.AddScoped<IMapper, Mapper>();
            services.AddCraftControllers().AddApplicationPart(typeof(ItemsController).Assembly);
        }).Configure(app =>
        {
            app.UseExceptionHandler(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization(); app.UseEndpoints(e => e.MapControllers());
        })).StartAsync(TestContext.Current.CancellationToken);
        using var scope = host.Services.CreateScope(); await scope.ServiceProvider.GetRequiredService<Store>().Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        return new Api(host, connection);
    }
    public async Task<HttpResponseMessage> Send(HttpMethod method, string path, object? body = null, string? tag = null)
    {
        using var request = new HttpRequestMessage(method, "/api/" + path);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (tag is not null) request.Headers.TryAddWithoutValidation("If-Match", tag);
        return await Client.SendAsync(request, TestContext.Current.CancellationToken);
    }
    public async Task<Output> Create(string name)
    {
        using var response = await Send(HttpMethod.Post, "items", new Input(name));
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        return (await response.Content.ReadFromJsonAsync<Output>(TestContext.Current.CancellationToken))!;
    }
    public async ValueTask DisposeAsync() { Client.Dispose(); await Host.StopAsync(TestContext.Current.CancellationToken); Host.Dispose(); await _connection.DisposeAsync(); }
}

[ApiController]
[Route("api/unrelated")]
public sealed class UnrelatedController : ControllerBase
{
    [HttpPost]
    public IActionResult Post([FromBody] string?[] values) => Ok(values.Length);
}
