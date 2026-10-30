using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace Craft.Auditing.Tests;

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

public sealed class CancelAuditWrite(CancellationTokenSource cancellation) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context!.ChangeTracker.Entries<AuditTrail>().Any(e => e.State == EntityState.Added))
            cancellation.Cancel();
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(result);
    }
}
