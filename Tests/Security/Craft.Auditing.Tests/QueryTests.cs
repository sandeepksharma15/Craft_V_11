using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Craft.Auditing.Tests;

public class QueryTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task EntityQueries_MatchExactCompositeKeysAndTenant()
    {
        using var f = new AuditFixture();
        f.Db.AddRange(new CompositeEntity { Region = "A", Number = 1 }, new CompositeEntity { Region = "A", Number = 10 });
        await f.Db.SaveChangesAsync(Token);
        var query = f.Db.AuditTrails.ForEntity(typeof(CompositeEntity).FullName!,
            new Dictionary<string, object?> { ["Number"] = 1, ["Region"] = "A" }, "tenant-1");
        Assert.Single(await query.ToListAsync(Token));
        Assert.Empty(await f.Db.AuditTrails.ForEntity(typeof(CompositeEntity).FullName!,
            new Dictionary<string, object?> { ["Number"] = 1, ["Region"] = "A" }, null).ToListAsync(Token));
        Assert.Empty(await f.Db.AuditTrails.ForEntity(typeof(CompositeEntity).FullName!,
            new Dictionary<string, object?> { ["Number"] = 1, ["Region"] = "a" }, "tenant-1").ToListAsync(Token));
        var now = f.Clock.GetUtcNow().UtcDateTime;
        Assert.Equal(2, await f.Db.AuditTrails.InPeriod(now, now.AddSeconds(1)).MostRecentFirst().CountAsync(Token));
        Assert.Empty(await f.Db.AuditTrails.InPeriod(now.AddSeconds(-1), now).ToListAsync(Token));
        Assert.Equal(2, (await f.Db.AuditTrails.MostRecentFirst().ToListAsync(Token)).Count);
    }

    [Fact]
    public void Keys_AreCanonicalAndRejectMissingIdentity()
    {
        var a = new Dictionary<string, object?> { ["B"] = Guid.Empty, ["A"] = -42L };
        var b = new Dictionary<string, object?> { ["A"] = -42L, ["B"] = Guid.Empty };
        Assert.Equal(AuditKey.Serialize(a), AuditKey.Serialize(b));
        Assert.Throws<ArgumentNullException>(() => AuditKey.Serialize(null!));
        Assert.Throws<ArgumentException>(() => AuditKey.Serialize(new Dictionary<string, object?>()));
        Assert.Throws<ArgumentException>(() => AuditKey.Serialize(new Dictionary<string, object?> { [" "] = 1 }));
        Assert.Throws<ArgumentException>(() => AuditKey.Serialize(new Dictionary<string, object?> { ["Id"] = null }));
    }

    [Fact]
    public void QueryGuards_RejectInvalidRangesAndArguments()
    {
        IQueryable<AuditTrail> query = Array.Empty<AuditTrail>().AsQueryable();
        var now = DateTime.UtcNow;
        Assert.Throws<ArgumentNullException>(() => ((IQueryable<AuditTrail>)null!).ForEntity("x", new Dictionary<string, object?>(), null));
        Assert.Throws<ArgumentNullException>(() => ((IQueryable<AuditTrail>)null!).InPeriod(now, now));
        Assert.Throws<ArgumentNullException>(() => ((IQueryable<AuditTrail>)null!).MostRecentFirst());
        Assert.Throws<ArgumentException>(() => query.ForEntity(" ", new Dictionary<string, object?>(), null));
        Assert.Throws<ArgumentException>(() => query.InPeriod(DateTime.SpecifyKind(now, DateTimeKind.Unspecified), now));
        Assert.Throws<ArgumentException>(() => query.InPeriod(now, DateTime.SpecifyKind(now, DateTimeKind.Local)));
        Assert.Throws<ArgumentException>(() => query.InPeriod(now, now.AddSeconds(-1)));
    }

    [Theory]
    [InlineData(null, null, 0, false)]
    [InlineData("{\"A\":null}", "{\"A\":\"\"}", 1, true)]
    [InlineData("{\"A\":1}", "{\"A\":\"1\"}", 1, true)]
    [InlineData("{\"A\":1}", "{\"A\":1}", 1, false)]
    [InlineData("{\"A\":{\"x\":1,\"y\":2}}", "{\"A\":{\"y\":2,\"x\":1}}", 1, false)]
    [InlineData(null, "{\"A\":null}", 1, true)]
    [InlineData("{\"A\":null}", null, 1, true)]
    public void Differences_PreserveJsonTypesAndMissingValues(string? oldValues, string? newValues, int count, bool changed)
    {
        var changes = new AuditTrail { OldValues = oldValues, NewValues = newValues }.GetDifferences();
        Assert.Equal(count, changes.Count);
        foreach (var change in changes)
        {
            Assert.Equal("A", change.PropertyName);
            Assert.Equal(changed, change.HasChanged);
            _ = change.OldValue; _ = change.NewValue;
        }
        Assert.False(new AuditDifference("A", null, null).HasChanged);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("invalid")]
    public void CorruptAuditJson_IsReported(string json)
        => Assert.Throws<JsonException>(() => new AuditTrail { OldValues = json }.GetDifferences());

    [Fact]
    public void NullAudit_IsRejected()
        => Assert.Throws<ArgumentNullException>(() => ((AuditTrail)null!).GetDifferences());
}