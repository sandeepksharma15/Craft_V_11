using System.Text.Json;
using Craft.Auditing.Capture;
using Craft.Auditing.Model;
using Craft.Auditing.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace Craft.Auditing.Tests;

public class PersistenceTests
{
    #region Private Properties

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    #endregion Private Properties

    #region Private Methods

    private static Task<int> Save(TestDbContext db, bool async) => async ? db.SaveChangesAsync(Token) : Task.FromResult(db.SaveChanges());

    #endregion Private Methods

    #region Public Methods

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task AuditWriteFailure_RollsBackBusinessChanges(bool async, bool external)
    {
        using var f = new AuditFixture();
        using var transaction = external ? f.Db.Database.BeginTransaction() : null;
        if (external)
        {
            f.Db.Add(new TestEntity { Name = "earlier write" });
            await Save(f.Db, async);
        }
        await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_audit BEFORE INSERT ON AuditTrails BEGIN SELECT RAISE(ABORT, 'audit failure'); END;", Token);
        f.Db.Add(new TestEntity { Name = "must roll back" });
        await Assert.ThrowsAsync<DbUpdateException>(() => Save(f.Db, async));
        Assert.Empty(f.Db.ChangeTracker.Entries());
        Assert.Equal(external ? 1 : 0, await f.Db.Set<TestEntity>().CountAsync(Token));
        Assert.Equal(external ? 1 : 0, await f.Db.AuditTrails.CountAsync(Token));
        if (external)
        {
            Assert.Same(transaction, f.Db.Database.CurrentTransaction);
            transaction!.Commit();
        }
    }

    [Fact]
    public async Task CancellationDuringAuditWrite_RollsBackTheBusinessWrite()
    {
        using var f = new AuditFixture();
        using var cancellation = new CancellationTokenSource();
        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(f.Connection)
            .AddInterceptors(new CancelAuditWrite(cancellation)).Options;
        using var db = new TestDbContext(options, new AuditTrailFactory(f.Actor, Options.Create(f.Options), f.Clock));
        db.Add(new TestEntity());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => db.SaveChangesAsync(cancellation.Token));
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Equal(0, await f.Db.Set<TestEntity>().CountAsync(Token));
        Assert.Equal(0, await f.Db.AuditTrails.CountAsync(Token));
    }

    [Fact]
    public async Task CancelledSave_DoesNotPersistAnything()
    {
        using var f = new AuditFixture();
        f.Db.Add(new TestEntity());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.Db.SaveChangesAsync(new CancellationToken(true)));
        Assert.Equal(0, await f.Db.AuditTrails.CountAsync(Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrencyFailure_DoesNotCreateAnAudit(bool async)
    {
        using var f = new AuditFixture();
        var entity = new TestEntity { Name = "before" };
        f.Db.Add(entity); await Save(f.Db, async);
        using (var other = f.Open())
        {
            var loaded = await other.Set<TestEntity>().SingleAsync(Token);
            loaded.Name = "winner";
            await Save(other, async);
        }
        entity.Name = "loser";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => Save(f.Db, async));
        using var check = f.Open();
        Assert.Equal("winner", (await check.Set<TestEntity>().SingleAsync(Token)).Name);
        Assert.Equal(2, await check.AuditTrails.CountAsync(Token));
    }

    [Fact]
    public void ExistingTransactionWithoutSavepoints_IsRejectedBeforeWriting()
    {
        using var f = new AuditFixture();
        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(f.Connection)
            .ReplaceService<IRelationalTransactionFactory, NoSavepointFactory>().Options;
        using var db = new TestDbContext(options, new AuditTrailFactory(f.Actor, Options.Create(f.Options), f.Clock));
        using var transaction = db.Database.BeginTransaction();
        db.Add(new TestEntity());
        Assert.Throws<NotSupportedException>(() => db.SaveChanges());
        Assert.Equal(0, db.Set<TestEntity>().Count());
    }

    [Fact]
    public async Task ExplicitlyModifiedButEqualValue_DoesNotCreateAnAudit()
    {
        using var f = new AuditFixture();
        var entity = new TestEntity { Name = "same" };
        f.Db.Add(entity); await f.Db.SaveChangesAsync(Token);
        f.Db.Entry(entity).Property(e => e.Name).IsModified = true;
        await f.Db.SaveChangesAsync(Token);
        Assert.Equal(1, await f.Db.AuditTrails.CountAsync(Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExternalTransaction_OwnsTheFinalCommit(bool async)
    {
        using var f = new AuditFixture();
        using var transaction = f.Db.Database.BeginTransaction();
        f.Db.Add(new TestEntity());
        await Save(f.Db, async);
        Assert.Same(transaction, f.Db.Database.CurrentTransaction);
        transaction.Rollback();
        using var verification = f.Open();
        Assert.Equal(0, await verification.AuditTrails.CountAsync(Token));
        Assert.Equal(0, await verification.Set<TestEntity>().CountAsync(Token));
    }

    [Fact]
    public async Task GeneratedGuidAndUtcTimes_RoundTripThroughDatabase()
    {
        using var f = new AuditFixture();
        var entity = new GuidEntity();
        f.Db.Add(entity);
        await f.Db.SaveChangesAsync(Token);
        using var check = f.Open();
        var audit = await check.AuditTrails.SingleAsync(Token);
        Assert.Equal(entity.Id, JsonElement.Parse(audit.KeyValues).GetProperty("Id").GetGuid());
        Assert.Equal(DateTimeKind.Utc, audit.OccurredAtUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, audit.ArchiveAfterUtc!.Value.Kind);
        entity.Value = "changed";
        await f.Db.SaveChangesAsync(Token);
        Assert.Equal(2, await check.AuditTrails.CountAsync(Token));
    }

    [Fact]
    public async Task InheritedAttributes_ProtectOverriddenProperties()
    {
        using var f = new AuditFixture();
        var entity = new InheritedEntity { Secret = "sensitive", Password = "private" };
        f.Db.Add(entity);
        await f.Db.SaveChangesAsync(Token);
        var audit = await f.Db.AuditTrails.SingleAsync(Token);
        Assert.Equal(typeof(InheritedEntity).FullName, audit.EntityName);
        var values = JsonElement.Parse(audit.NewValues!);
        Assert.Equal("[REDACTED]", values.GetProperty("Secret").GetString());
        Assert.False(values.TryGetProperty("Password", out _));
    }

    [Fact]
    public async Task ManualAuditWritesAndDeferredAcceptance_AreRejected()
    {
        using var f = new AuditFixture();
        Assert.Throws<NotSupportedException>(() => f.Db.SaveChanges(false));
        await Assert.ThrowsAsync<NotSupportedException>(() => f.Db.SaveChangesAsync(false, Token));
        f.Db.Add(new AuditTrail());
        Assert.Throws<InvalidOperationException>(() => f.Db.SaveChanges());
    }

    [Fact]
    public async Task NullIdentity_IsRejectedBeforeAnyWrite()
    {
        using var f = new AuditFixture();
        f.Actor.Identity = null!;
        f.Db.Add(new TestEntity());
        await Assert.ThrowsAsync<ArgumentNullException>(() => f.Db.SaveChangesAsync(Token));
    }

    [Fact]
    public async Task OptInAndExclusions_AreAppliedWithoutAssemblyScanning()
    {
        using var f = new AuditFixture();
        var selected = new SelectedEntity { Value = "included", NotSelected = "hidden", Secret = "excluded" };
        f.Db.AddRange(selected, new ExcludedEntity(), new PlainEntity());
        await f.Db.SaveChangesAsync(Token);
        var audit = await f.Db.AuditTrails.SingleAsync(Token);
        Assert.Equal(typeof(SelectedEntity).FullName, audit.EntityName);
        Assert.Equal("{\"Value\":\"included\"}", audit.NewValues);
        selected.NotSelected = "changed"; selected.Secret = "changed";
        await f.Db.SaveChangesAsync(Token);
        Assert.Equal(1, await f.Db.AuditTrails.CountAsync(Token));
        selected.Value = "changed";
        await f.Db.SaveChangesAsync(Token);
        Assert.Equal(2, await f.Db.AuditTrails.CountAsync(Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Save_RecordsFinalKeysNullsShadowValuesIdentityAndRetention(bool async)
    {
        using var f = new AuditFixture();
        var entity = new TestEntity { Name = null, Password = "password", Secret = "secret" };
        f.Db.Add(entity);
        f.Db.Entry(entity).Property("Shadow").CurrentValue = "shadow";
        Assert.Equal(1, await Save(f.Db, async));
        Assert.True(entity.Id > 0);
        var audit = await f.Db.AuditTrails.SingleAsync(Token);
        Assert.Equal(AuditOperation.Created, audit.Operation);
        Assert.Equal(f.Clock.GetUtcNow().UtcDateTime, audit.OccurredAtUtc);
        Assert.Equal(audit.OccurredAtUtc.AddDays(30), audit.ArchiveAfterUtc);
        Assert.Equal("user-1", audit.UserId);
        Assert.Equal("tenant-1", audit.TenantId);
        Assert.Equal("request-1", audit.CorrelationId);
        Assert.False(audit.IsArchived);
        Assert.Null(audit.OldValues);
        Assert.Equal(entity.Id, JsonElement.Parse(audit.KeyValues).GetProperty("Id").GetInt32());
        var values = JsonElement.Parse(audit.NewValues!);
        Assert.Equal(entity.Id, values.GetProperty("Id").GetInt32());
        Assert.Equal(JsonValueKind.Null, values.GetProperty("Name").ValueKind);
        Assert.Equal("shadow", values.GetProperty("Shadow").GetString());
        Assert.Equal("[REDACTED]", values.GetProperty("Secret").GetString());
        Assert.False(values.TryGetProperty("Password", out _));
        Assert.Equal(0, await Save(f.Db, async));
        Assert.Equal(1, await f.Db.AuditTrails.CountAsync(Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdatesAndDeletion_PreserveBusinessSemantics(bool async)
    {
        using var f = new AuditFixture();
        var entity = new TestEntity { Name = null, Secret = "one", Data = [1, 2] };
        f.Db.Add(entity); await Save(f.Db, async);
        entity.Name = ""; entity.Secret = "two"; entity.Data = [1, 2];
        await Save(f.Db, async);
        var update = await f.Db.AuditTrails.SingleAsync(a => a.Operation == AuditOperation.Updated, Token);
        Assert.Contains("Name", update.ChangedProperties);
        Assert.DoesNotContain("Data", update.ChangedProperties);
        Assert.Equal(JsonValueKind.Null, JsonElement.Parse(update.OldValues!).GetProperty("Name").ValueKind);
        Assert.Equal("[REDACTED]", JsonElement.Parse(update.OldValues!).GetProperty("Secret").GetString());
        entity.IsDeleted = true; await Save(f.Db, async);
        entity.Name = "still deleted"; await Save(f.Db, async);
        entity.IsDeleted = false; await Save(f.Db, async);
        Assert.Equal(1, await f.Db.AuditTrails.CountAsync(a => a.Operation == AuditOperation.SoftDeleted, Token));
        Assert.Equal(1, await f.Db.AuditTrails.CountAsync(a => a.Operation == AuditOperation.Restored, Token));
        Assert.Equal(2, await f.Db.AuditTrails.CountAsync(a => a.Operation == AuditOperation.Updated, Token));
        f.Db.Remove(entity); await Save(f.Db, async);
        Assert.Empty(await f.Db.Set<TestEntity>().ToListAsync(Token));
        var deletion = await f.Db.AuditTrails.SingleAsync(a => a.Operation == AuditOperation.Deleted, Token);
        Assert.NotNull(deletion.OldValues);
        Assert.Null(deletion.NewValues);
    }

    #endregion Public Methods
}
