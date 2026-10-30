namespace Craft.Auditing.Model;

public sealed record AuditIdentity(string? UserId = null, string? TenantId = null, string? CorrelationId = null);
