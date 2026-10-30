using Craft.Auditing.Model;
using Microsoft.EntityFrameworkCore;

namespace Craft.Auditing;

public static class AuditArchiver
{
    /// <summary>Marks at most one batch archived. Schedule repeated calls in the application's job runner.</summary>
    public static Task<int> ArchiveDueAsync(DbContext context, DateTime nowUtc, int batchSize, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (nowUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("The cutoff must be UTC.", nameof(nowUtc));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
        return context.Set<AuditTrail>().Where(a => !a.IsArchived && a.ArchiveAfterUtc <= nowUtc)
            .OrderBy(a => a.ArchiveAfterUtc).ThenBy(a => a.Id).Take(batchSize)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.IsArchived, true), cancellationToken);
    }
}