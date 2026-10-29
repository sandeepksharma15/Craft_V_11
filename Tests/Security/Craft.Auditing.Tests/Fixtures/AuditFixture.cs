using Craft.Domain.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Craft.Auditing.Tests;

public sealed class TestAuditContext : IAuditContext
{
    public AuditIdentity Identity { get; set; } = new("user-1", "tenant-1", "request-1");
    public AuditIdentity GetIdentity() => Identity;
}

public sealed class TestClock : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(2026, 1, 2, 12, 0, 0, TimeSpan.Zero);
}

public sealed class AuditFixture : IDisposable
{
    public SqliteConnection Connection { get; } = new("Data Source=:memory:");
    public TestAuditContext Actor { get; } = new();
    public AuditOptions Options { get; } = new() { RetentionPeriod = TimeSpan.FromDays(30) };
    public TestClock Clock { get; } = new();
    public TestDbContext Db { get; }

    public AuditFixture()
    {
        Connection.Open();
        Db = Open();
        Db.Database.EnsureCreated();
    }

    public TestDbContext Open() => new(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(Connection).Options,
        new AuditTrailFactory(Actor, Microsoft.Extensions.Options.Options.Create(Options), Clock));

    public void Dispose() { Db.Dispose(); Connection.Dispose(); }
}

public sealed class TestDbContext(DbContextOptions<TestDbContext> options, AuditTrailFactory factory) : AuditedDbContext(options, factory)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TestEntity>().Property<string>("Shadow");
        modelBuilder.Entity<TestEntity>().Property(e => e.Name).IsConcurrencyToken();
        modelBuilder.Entity<GuidEntity>();
        modelBuilder.Entity<InheritedEntity>();
        modelBuilder.Entity<ExcludedEntity>();
        modelBuilder.Entity<PlainEntity>();
        modelBuilder.Entity<SelectedEntity>();
        modelBuilder.Entity<CompositeEntity>().HasKey(x => new { x.Region, x.Number });
    }
}

[Audit]
public class TestEntity : ISoftDelete
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsDeleted { get; set; }
    public byte[]? Data { get; set; }
    [DoNotAudit] public string? Password { get; set; }
    [AuditRedact] public string? Secret { get; set; }
}

[Audit, DoNotAudit]
public class ExcludedEntity { public int Id { get; set; } public string? Value { get; set; } }
public class PlainEntity { public int Id { get; set; } public string? Value { get; set; } }
public class SelectedEntity
{
    public int Id { get; set; }
    [Audit] public string? Value { get; set; }
    [Audit, DoNotAudit] public string? Secret { get; set; }
    public string? NotSelected { get; set; }
}
[Audit]
public class CompositeEntity
{
    public string Region { get; set; } = "";
    public int Number { get; set; }
    public string? Value { get; set; }
}

[Audit]
public class GuidEntity { public Guid Id { get; set; } public string? Value { get; set; } }

[Audit]
public abstract class AuditedBase
{
    public int Id { get; set; }
    [AuditRedact] public virtual string? Secret { get; set; }
    [DoNotAudit] public virtual string? Password { get; set; }
}

public class InheritedEntity : AuditedBase
{
    public override string? Secret { get; set; }
    public override string? Password { get; set; }
}
