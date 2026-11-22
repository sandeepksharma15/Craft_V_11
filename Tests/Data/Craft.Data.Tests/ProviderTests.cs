using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Craft.Data.Tests;

public class ProviderTests
{
    public static TheoryData<string> Providers()
    {
        TheoryData<string> providers = new() { "Sqlite" };

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CRAFT_TEST_SQLSERVER")))
            providers.Add("SqlServer");

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CRAFT_TEST_POSTGRESQL"))
            || Environment.GetEnvironmentVariable("CRAFT_REQUIRE_POSTGRESQL") == "1")
            providers.Add("PostgreSql");

        return providers;
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task Provider_PersistsFiltersConcurrencyAndAtomicAudit(string provider)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        DbContextOptionsBuilder<DataContext> options = new();

        switch (provider)
        {
            case "Sqlite":
                options.UseSqlite("Data Source=:memory:");
                break;
            case "SqlServer":
                string sqlServer = GetConnection("CRAFT_TEST_SQLSERVER");
                Assert.StartsWith("craft_test_", new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(sqlServer).InitialCatalog);
                options.UseSqlServer(sqlServer);
                break;
            case "PostgreSql":
                string postgres = GetConnection("CRAFT_TEST_POSTGRESQL");
                NpgsqlConnectionStringBuilder connection = new(postgres);
                Assert.Equal("craft_test_data_postgresql", connection.Database);
                options.UseNpgsql(postgres);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider));
        }

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

    private static string GetConnection(string variable)
    {
        string? connection = Environment.GetEnvironmentVariable(variable);

        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException($"{variable} must point to a disposable test database.");

        return connection;
    }
}
