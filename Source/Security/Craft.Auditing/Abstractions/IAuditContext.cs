namespace Craft.Auditing;

/// <summary>Provides trusted request or background-job identity, independent of HTTP.</summary>
public interface IAuditContext
{
    AuditIdentity GetIdentity();
}