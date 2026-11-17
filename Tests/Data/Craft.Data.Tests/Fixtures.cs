using Craft.Auditing.Abstractions;
using Craft.Auditing.Attributes;
using Craft.Auditing.Capture;
using Craft.Auditing.Configuration;
using Craft.Auditing.Model;
using Craft.Auditing.Persistence;
using Craft.Core.Abstractions;
using Craft.Domain.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Craft.Data.Tests;

[Audit]
public class Document : ISoftDelete, IHasConcurrency, IHasVersion, IHasTenant<string>
{
    public int Id { get; set; }
    public string TenantId { get; set; } = null!;
    public string Name { get; set; } = "original";
    public bool IsDeleted { get; set; }
    public string? ConcurrencyStamp { get; set; }
    public long Version { get; set; }
}

public sealed class Tenant(string id) : ICurrentTenant<string>
{
    public string Id { get; set; } = id;
    public string? Identifier => Id;
    public bool IsAvailable { get; set; } = true;
    public bool TryGetId(out string id) { id = Id; return IsAvailable; }
}

public sealed class Actor : IAuditContext
{
    public AuditIdentity GetIdentity() => new("actor", "tenant", "request");
}

public sealed class DataContext(DbContextOptions<DataContext> options) : CraftDbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>().Property(d => d.Name).IsRequired();
        modelBuilder.Entity<Document>().HasQueryFilter("Application", d => d.Name != "hidden");
    }
}

public sealed class DataFixture : IDisposable
{
    public SqliteConnection Connection { get; } = new("Data Source=:memory:");
    public DataFixture()
    {
        Connection.Open();
        using var context = Open();
        context.Database.EnsureCreated();
    }

    public DataContext Open(string tenant = "a", params PersistenceFeature[] extra)
    {
        var options = new DbContextOptionsBuilder<DataContext>().UseSqlite(Connection);
        options.UseCraftData([.. Features(tenant), .. extra]);
        return new DataContext(options.Options);
    }

    public static PersistenceFeature[] Features(string tenant) =>
    [
        new AuditingFeature(new AuditTrailFactory(new Actor(), Options.Create(new AuditOptions()), TimeProvider.System)),
        new VersionFeature(), new ConcurrencyFeature(), new SoftDeleteFeature(), new TenantFeature<string>(new Tenant(tenant))
    ];

    public void Dispose() => Connection.Dispose();
}
