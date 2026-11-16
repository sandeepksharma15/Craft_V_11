namespace Craft.Authentication;

/// <summary>Store opaque revocation keys until the supplied UTC deadline, across all validating instances.</summary>
public interface ITokenRevocationStore
{
    Task<bool> IsRevokedAsync(string key, CancellationToken cancellationToken = default);
    Task RevokeAsync(string key, DateTimeOffset retainUntil, CancellationToken cancellationToken = default);
}
