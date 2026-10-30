namespace Craft.Auditing;

public sealed class AuditTrail
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string EntityName { get; init; } = string.Empty;
    public string EntityKey { get; init; } = string.Empty;
    public string KeyValues { get; init; } = "{}";
    public AuditOperation Operation { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public string? UserId { get; init; }
    public string? TenantId { get; init; }
    public string? CorrelationId { get; init; }
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string ChangedProperties { get; init; } = "[]";
    public DateTime? ArchiveAfterUtc { get; init; }
    public bool IsArchived { get; internal set; }
}