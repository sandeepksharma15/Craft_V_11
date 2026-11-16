namespace Craft.Identity;

/// <summary>Persistence contract. Store only the token hash; the secret is returned to the caller once.</summary>
public sealed record RefreshSession<TKey>(Guid Id, Guid FamilyId, TKey UserId, string TokenHash,
    DateTimeOffset ExpiresAt, DateTimeOffset? ConsumedAt = null, DateTimeOffset? RevokedAt = null) where TKey : notnull;

public enum RefreshRotationStatus { Rotated, Invalid, Expired, Reused, Revoked }

public sealed record RefreshRotationResult<TKey>(RefreshRotationStatus Status, TKey? UserId) where TKey : notnull;

public interface IRefreshSessionStore<TKey> where TKey : notnull
{
    Task CreateAsync(RefreshSession<TKey> session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically consume the presented hash and create its replacement for the same user/family.
    /// Reject expired/revoked sessions; reuse revokes the entire family. Return a user only when rotated.
    /// Persist expiry checks using now; replacement expiry must not extend the family's absolute lifetime.
    /// </summary>
    Task<RefreshRotationResult<TKey>> RotateAsync(string presentedHash, string replacementHash, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
