using System.Collections.Concurrent;

namespace Craft.Authentication;

/// <summary>Single-process development store; revocations are lost on restart.</summary>
public sealed class InMemoryTokenRevocationStore(TimeProvider clock) : ITokenRevocationStore
{
    private readonly TimeProvider _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly ConcurrentDictionary<string, DateTimeOffset> _entries = new(StringComparer.Ordinal);

    public Task<bool> IsRevokedAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_entries.TryGetValue(key, out var deadline) && deadline > _clock.GetUtcNow());
    }

    public Task RevokeAsync(string key, DateTimeOffset retainUntil, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();
        var now = _clock.GetUtcNow();
        foreach (var entry in _entries)
            if (entry.Value <= now) ((ICollection<KeyValuePair<string, DateTimeOffset>>)_entries).Remove(entry);
        if (retainUntil > now)
            _entries.AddOrUpdate(key, retainUntil, (_, old) => old > retainUntil ? old : retainUntil);
        return Task.CompletedTask;
    }
}
