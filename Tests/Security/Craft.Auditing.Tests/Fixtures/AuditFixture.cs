using Craft.Auditing.Abstractions;
using Craft.Auditing.Attributes;
using Craft.Auditing.Capture;
using Craft.Auditing.Configuration;
using Craft.Auditing.Model;
using Craft.Auditing.Persistence;
using Craft.Domain.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Craft.Auditing.Tests.Fixtures;

[Audit]
public abstract class AuditedBase
{
    #region Public Properties

    public int Id { get; set; }
    [DoNotAudit] public virtual string? Password { get; set; }
    [AuditRedact] public virtual string? Secret { get; set; }

    #endregion Public Properties
}

public sealed class AuditFixture : IDisposable
{
    #region Public Constructors

    public AuditFixture()
    {
        Connection.Open();
        Db = Open();
        Db.Database.EnsureCreated();
    }

    #endregion Public Constructors

    #region Public Properties

    public TestAuditContext Actor { get; } = new();
    public TestClock Clock { get; } = new();
    public SqliteConnection Connection { get; } = new("Data Source=:memory:");
    public TestDbContext Db { get; }
    public AuditOptions Options { get; } = new() { RetentionPeriod = TimeSpan.FromDays(30) };

    #endregion Public Properties

    #region Public Methods

    public void Dispose() { Db.Dispose(); Connection.Dispose(); }

    public TestDbContext Open() => new(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(Connection).Options,
            new AuditTrailFactory(Actor, Microsoft.Extensions.Options.Options.Create(Options), Clock));

    #endregion Public Methods
}

[Audit]
public class CompositeEntity
{
    #region Public Properties

    public int Number { get; set; }
    public string Region { get; set; } = "";
    public string? Value { get; set; }

    #endregion Public Properties
}

[Audit, DoNotAudit]
public class ExcludedEntity
{
    #region Public Properties

    public int Id { get; set; }
    public string? Value { get; set; }

    #endregion Public Properties
}

[Audit]
public class GuidEntity
{
    #region Public Properties

    public Guid Id { get; set; }
    public string? Value { get; set; }

    #endregion Public Properties
}

public class PlainEntity
{
    #region Public Properties

    public int Id { get; set; }
    public string? Value { get; set; }

    #endregion Public Properties
}

public class SelectedEntity
{
    #region Public Properties

    public int Id { get; set; }
    public string? NotSelected { get; set; }
    [Audit, DoNotAudit] public string? Secret { get; set; }
    [Audit] public string? Value { get; set; }

    #endregion Public Properties
}

public sealed class TestAuditContext : IAuditContext
{
    #region Public Properties

    public AuditIdentity Identity { get; set; } = new("user-1", "tenant-1", "request-1");

    #endregion Public Properties

    #region Public Methods

    public AuditIdentity GetIdentity() => Identity;

    #endregion Public Methods
}

public sealed class TestClock : TimeProvider
{
    #region Public Methods

    public override DateTimeOffset GetUtcNow() => new(2026, 1, 2, 12, 0, 0, TimeSpan.Zero);

    #endregion Public Methods
}

public sealed class TestDbContext(DbContextOptions<TestDbContext> options, AuditTrailFactory factory) : AuditedDbContext(options, factory)
{
    #region Protected Methods

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

    #endregion Protected Methods
}

[Audit]
public class TestEntity : ISoftDelete
{
    #region Public Properties

    public byte[]? Data { get; set; }
    public int Id { get; set; }
    public bool IsDeleted { get; set; }
    public string? Name { get; set; }
    [DoNotAudit] public string? Password { get; set; }
    [AuditRedact] public string? Secret { get; set; }

    #endregion Public Properties
}

public class InheritedEntity : AuditedBase
{
    #region Public Properties

    public override string? Password { get; set; }
    public override string? Secret { get; set; }

    #endregion Public Properties
}
