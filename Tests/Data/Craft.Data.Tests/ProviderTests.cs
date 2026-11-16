using Microsoft.EntityFrameworkCore;

namespace Craft.Data.Tests;

public class ProviderTests
{
    public static IEnumerable<object[]> Providers()
    {
        yield return ["Sqlite", "Data Source=:memory:"];
        foreach (var provider in new[] { "SqlServer" })
            if (Environment.GetEnvironmentVariable("CRAFT_TEST_" + provider.ToUpperInvariant()) is { Length: > 0 } connection)
                yield return [provider, connection];
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Provider_PersistsFiltersConcurrencyAndAtomicAudit(string provider, string connection)
    {
        var token = TestContext.Current.CancellationToken;
        if (provider != "Sqlite")
        {
            var database = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection).InitialCatalog;
            Assert.StartsWith("craft_test_", database);
        }
        var options = new DbContextOptionsBuilder<DataContext>();
        if (provider == "SqlServer") options.UseSqlServer(connection);
        else options.UseSqlite(connection);
        options.UseCraftData(DataFixture.Features("tenant"));
        await using var db = new DataContext(options.Options);
        if (provider == "Sqlite") await db.Database.OpenConnectionAsync(token);
        await db.Database.EnsureDeletedAsync(token);
        await db.Database.EnsureCreatedAsync(token);
        try
        {
            var entity = new Document();
            db.Add(entity);
            await db.SaveChangesAsync(token);
            Assert.True(entity.Id > 0);
            db.Remove(entity);
            await db.SaveChangesAsync(token);
            db.ChangeTracker.Clear();
            Assert.Empty(await db.Documents.ToListAsync(token));
            Assert.Single(await db.Documents.IncludeDeleted().ToListAsync(token));
            Assert.Equal(2, await db.AuditTrails.CountAsync(token));
            var row = await db.Documents.IncludeDeleted().SingleAsync(token);
            db.Restore(row);
            await db.SaveChangesAsync(token);
            Assert.Equal(3, row.Version);
            await using var transaction = await db.Database.BeginTransactionAsync(token);
            db.Add(new Document());
            await db.SaveChangesAsync(token);
            await transaction.RollbackAsync(token);
            db.ChangeTracker.Clear();
            Assert.Single(await db.Documents.ToListAsync(token));
            Assert.Equal(3, await db.AuditTrails.CountAsync(token));
        }
        finally { await db.Database.EnsureDeletedAsync(CancellationToken.None); }
    }
}
