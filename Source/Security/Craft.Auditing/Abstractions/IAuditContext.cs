using Craft.Auditing.Model;

namespace Craft.Auditing.Abstractions;

/// <summary>
/// Provides trusted request or background-job identity, independent of HTTP.
/// </summary>
public interface IAuditContext
{
    #region Public Methods

    AuditIdentity GetIdentity();

    #endregion Public Methods
}
