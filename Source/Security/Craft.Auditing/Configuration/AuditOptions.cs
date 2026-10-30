namespace Craft.Auditing.Configuration;

public sealed class AuditOptions
{
    #region Public Properties

    public string RedactionText { get; set; } = "[REDACTED]";
    public TimeSpan? RetentionPeriod { get; set; }

    #endregion Public Properties
}
