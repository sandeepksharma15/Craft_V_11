namespace Craft.Auditing;

public sealed class AuditOptions
{
    public TimeSpan? RetentionPeriod { get; set; }
    public string RedactionText { get; set; } = "[REDACTED]";
}