using Craft.Auditing.Abstractions;
using Craft.Auditing.Attributes;
using Craft.Auditing.Capture;
using Craft.Auditing.Configuration;
using Craft.Auditing.Model;
using Craft.Auditing.Persistence;
using Craft.Core.Abstractions;
using Craft.Data;
using Craft.Domain.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Craft.Repositories.Tests;

[Audit]
public class Product : IEntity<int>, ISoftDelete, IHasTenant<string>, IHasVersion, IHasConcurrency
{
    public int Id { get; set; }
    public string Name { get; set; } = "product";
    public string TenantId { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public long Version { get; set; }
    public string? ConcurrencyStamp { get; set; }
    public List<Detail> Details { get; set; } = [];
}
public sealed class Detail : IHasTenant<string>
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string TenantId { get; set; } = null!;
    public string Text { get; set; } = "detail";
}
public sealed class Note : IEntity { public KeyType Id { get; set; } public string Text { get; set; } = "note"; }
public sealed class Tag : IEntity<Guid> { public Guid Id { get; set; } public string Text { get; set; } = "tag"; }
public sealed class Code : IEntity<string> { public string Id { get; set; } = "code"; }

public sealed class ShopContext(DbContextOptions<ShopContext> options) : CraftDbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Code> Codes => Set<Code>();
    public DbSet<AuditTrail> Audits => Set<AuditTrail>();
    protected override void ConfigureModel(ModelBuilder model)
    {
        model.Entity<Product>().HasQueryFilter("Application", p => p.Name != "hidden");
        model.Entity<Detail>().HasOne<Product>().WithMany(p => p.Details)
            .HasForeignKey(d => new { d.TenantId, d.ProductId }).HasPrincipalKey(p => new { p.TenantId, p.Id }).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Product>().Navigation(p => p.Details).AutoInclude();
    }
}
public sealed class DetailsRepository(ShopContext db, IOptions<RepositoryOptions> options) : Repository<Product, int, ShopContext>(db, options)
{
    protected override IQueryable<Product> ApplyDetails(IQueryable<Product> query) => query.Include(p => p.Details);
}
public sealed class CurrentTenant(string id) : ICurrentTenant<string>
{
    public string? Identifier => id;
    public bool IsAvailable => true;
    public bool TryGetId(out string value) { value = id; return true; }
}
public sealed class Actor : IAuditContext
{
    public AuditIdentity GetIdentity() => new("test-user", "tenant", "test-request");
}
public sealed class Fixture : IDisposable
{
    public SqliteConnection Connection { get; } = new("Data Source=:memory:");
    public static IOptions<RepositoryOptions> Limits => Options.Create(new RepositoryOptions());
    public Fixture() { Connection.Open(); using var db = Open(); db.Database.EnsureCreated(); }
    public ShopContext Open(string tenant = "a", bool softDelete = true)
    {
        var options = new DbContextOptionsBuilder<ShopContext>().UseSqlite(Connection);
        options.UseCraftData(Features(tenant, softDelete));
        return new ShopContext(options.Options);
    }
    public static PersistenceFeature[] Features(string tenant = "a", bool softDelete = true)
    {
        List<PersistenceFeature> features = [new TenantFeature<string>(new CurrentTenant(tenant)), new VersionFeature(), new ConcurrencyFeature(),
            new AuditingFeature(new AuditTrailFactory(new Actor(), Options.Create(new AuditOptions()), TimeProvider.System))];
        if (softDelete) features.Add(new SoftDeleteFeature());
        return [.. features];
    }
    public void Dispose() => Connection.Dispose();
}
