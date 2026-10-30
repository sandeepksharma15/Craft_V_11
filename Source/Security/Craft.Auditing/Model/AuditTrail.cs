namespace Craft.Auditing.Model;

public sealed class AuditTrail
{
    #region Public Properties

    public DateTime? ArchiveAfterUtc { get; init; }
    public string ChangedProperties { get; init; } = "[]";
    public string? CorrelationId { get; init; }
    public string EntityKey { get; init; } = string.Empty;
    public string EntityName { get; init; } = string.Empty;
    public Guid Id { get; init; } = Guid.NewGuid();
    public bool IsArchived { get; internal set; }
    public string KeyValues { get; init; } = "{}";
    public string? NewValues { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public string? OldValues { get; init; }
    public AuditOperation Operation { get; init; }
    public string? TenantId { get; init; }
    public string? UserId { get; init; }

    #endregion Public Properties
}
