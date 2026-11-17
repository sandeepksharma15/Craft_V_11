using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace Craft.Data.Tests;

public class TransactionTests
{
    [Fact]
    public void NonRelationalProvider_IsRejected()
    {
        var options = new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase(Guid.NewGuid().ToString());
        options.UseCraftData(DataFixture.Features("a"));
        using var db = new DataContext(options.Options);
        Assert.Throws<NotSupportedException>(() => db.SaveChanges());
    }

    [Fact]
    public void ExistingTransactionWithoutSavepoints_IsRejected()
    {
        using var fixture = new DataFixture();
        var options = new DbContextOptionsBuilder<DataContext>().UseSqlite(fixture.Connection)
            .ReplaceService<IRelationalTransactionFactory, NoSavepointFactory>();
        options.UseCraftData(DataFixture.Features("a"));
        using var db = new DataContext(options.Options);
        using var transaction = db.Database.BeginTransaction();
        Assert.Throws<NotSupportedException>(() => db.SaveChanges());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SavepointCreationFailure_DoesNotAttemptNonexistentSavepointRollback(bool asynchronous)
    {
        using var fixture = new DataFixture();
        var options = new DbContextOptionsBuilder<DataContext>().UseSqlite(fixture.Connection).AddInterceptors(new FailSavepoint());
        options.UseCraftData(DataFixture.Features("a"));
        using var db = new DataContext(options.Options);
        await using var transaction = await db.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
        db.Add(new Document());
        var error = asynchronous
            ? await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken))
            : Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
        Assert.Equal("Savepoint failed", error.Message);
        await transaction.RollbackAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SuccessfulSavepoint_ParticipatesInOuterRollback(bool asynchronous)
    {
        using var fixture = new DataFixture();
        using var db = fixture.Open();
        await using var transaction = await db.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
        db.Add(new Document());
        if (asynchronous) await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        else db.SaveChanges();
        await transaction.RollbackAsync(TestContext.Current.CancellationToken);
        using var check = fixture.Open();
        Assert.Empty(await check.Documents.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await check.AuditTrails.ToListAsync(TestContext.Current.CancellationToken));
    }

    private sealed class FailSavepoint : DbTransactionInterceptor
    {
        public override InterceptionResult CreatingSavepoint(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result)
            => throw new InvalidOperationException("Savepoint failed");
        public override ValueTask<InterceptionResult> CreatingSavepointAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Savepoint failed");
    }
}

public sealed class NoSavepointFactory(RelationalTransactionFactoryDependencies dependencies) : RelationalTransactionFactory(dependencies)
{
    public override RelationalTransaction Create(IRelationalConnection connection, DbTransaction transaction,
        Guid transactionId, IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> logger, bool transactionOwned)
        => new NoSavepointTransaction(connection, transaction, transactionId, logger, transactionOwned, Dependencies.SqlGenerationHelper);
}

public sealed class NoSavepointTransaction(IRelationalConnection connection, DbTransaction transaction,
    Guid transactionId, IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> logger, bool transactionOwned,
    ISqlGenerationHelper sql) : RelationalTransaction(connection, transaction, transactionId, logger, transactionOwned, sql)
{
    public override bool SupportsSavepoints => false;
}
