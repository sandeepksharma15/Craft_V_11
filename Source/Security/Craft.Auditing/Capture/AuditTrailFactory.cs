using System.Reflection;
using System.Text.Json;
using Craft.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;

namespace Craft.Auditing;

public sealed class AuditTrailFactory
{
    private readonly IAuditContext _context;
    private readonly TimeProvider _time;
    private readonly TimeSpan? _retention;
    private readonly string _redaction;

    public AuditTrailFactory(IAuditContext context, IOptions<AuditOptions> options, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(time);
        var settings = options.Value;
        if (settings.RetentionPeriod <= TimeSpan.Zero)
            throw new ArgumentException("RetentionPeriod must be positive.", nameof(options));
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.RedactionText);
        _context = context;
        _time = time;
        _retention = settings.RetentionPeriod;
        _redaction = settings.RedactionText;
    }

    internal List<PendingAudit> Capture(DbContext db)
    {
        db.ChangeTracker.DetectChanges();
        var identity = _context.GetIdentity();
        ArgumentNullException.ThrowIfNull(identity);
        var now = _time.GetUtcNow().UtcDateTime;
        var pending = new List<PendingAudit>();
        foreach (var entry in db.ChangeTracker.Entries().ToArray())
        {
            if (entry.Entity is AuditTrail)
            {
                if (entry.State != EntityState.Unchanged)
                    throw new InvalidOperationException("Audit rows are append-only and managed by the save coordinator.");
                continue;
            }
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;
            var type = entry.Metadata.ClrType;
            if (type.IsDefined(typeof(DoNotAuditAttribute), true))
                continue;
            var wholeEntity = type.IsDefined(typeof(AuditAttribute), true);
            var properties = entry.Properties.Where(p =>
                !Has<DoNotAuditAttribute>(p) && (wholeEntity || Has<AuditAttribute>(p))).ToArray();
            if (!wholeEntity && properties.Length == 0)
                continue;

            var operation = GetOperation(entry);
            var selected = operation is AuditOperation.Created or AuditOperation.Deleted
                ? properties
                : properties.Where(p => p.IsModified && !p.Metadata.GetValueComparer()!.Equals(p.OriginalValue, p.CurrentValue)).ToArray();
            if (operation == AuditOperation.Updated && selected.Length == 0)
                continue;
            var oldValues = operation == AuditOperation.Created ? null : Serialize(selected);
            pending.Add(new PendingAudit(entry, selected, operation, oldValues, identity, now,
                _retention is { } retention ? now.Add(retention) : null, _redaction));
        }
        return pending;
    }

    private string Serialize(PropertyEntry[] properties)
        => JsonSerializer.Serialize(properties.ToDictionary(p => p.Metadata.Name,
            p => Has<AuditRedactAttribute>(p) ? _redaction : p.OriginalValue));

    internal static bool Has<T>(PropertyEntry property) where T : Attribute
        => property.Metadata.PropertyInfo is { } member && Attribute.IsDefined(member, typeof(T), inherit: true);

    private static AuditOperation GetOperation(EntityEntry entry)
    {
        if (entry.State == EntityState.Added) return AuditOperation.Created;
        if (entry.State == EntityState.Deleted) return AuditOperation.Deleted;
        if (entry.Entity is ISoftDelete)
        {
            var flag = entry.Property(nameof(ISoftDelete.IsDeleted));
            if (!Equals(flag.OriginalValue, flag.CurrentValue))
                return flag.CurrentValue is true ? AuditOperation.SoftDeleted : AuditOperation.Restored;
        }
        return AuditOperation.Updated;
    }
}

internal sealed record PendingAudit(EntityEntry Entry, PropertyEntry[] Properties, AuditOperation Operation,
    string? OldValues, AuditIdentity Identity, DateTime Timestamp, DateTime? ArchiveAfter, string Redaction)
{
    internal AuditTrail Complete()
    {
        var keys = Entry.Properties.Where(p => p.Metadata.IsPrimaryKey())
            .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
        var keyJson = AuditKey.Serialize(keys);
        return new AuditTrail
        {
            EntityName = Entry.Metadata.Name,
            EntityKey = AuditKey.Fingerprint(keyJson),
            KeyValues = keyJson,
            Operation = Operation,
            OccurredAtUtc = Timestamp,
            UserId = Identity.UserId,
            TenantId = Identity.TenantId,
            CorrelationId = Identity.CorrelationId,
            OldValues = OldValues,
            NewValues = Operation == AuditOperation.Deleted ? null : JsonSerializer.Serialize(Properties.ToDictionary(
                p => p.Metadata.Name, p => AuditTrailFactory.Has<AuditRedactAttribute>(p) ? Redaction : p.CurrentValue)),
            ChangedProperties = JsonSerializer.Serialize(Properties.Select(p => p.Metadata.Name).Order(StringComparer.Ordinal)),
            ArchiveAfterUtc = ArchiveAfter
        };
    }
}