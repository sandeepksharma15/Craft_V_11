using System.Diagnostics.CodeAnalysis;

namespace Craft.Core;

public interface ICurrentTenant<TKey> where TKey : notnull
{
    bool IsAvailable { get; }
    string? Identifier { get; }
    bool TryGetId([MaybeNullWhen(false)] out TKey id);
}

public interface ICurrentTenant : ICurrentTenant<KeyType>;