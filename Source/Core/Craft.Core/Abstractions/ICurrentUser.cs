using System.Diagnostics.CodeAnalysis;

namespace Craft.Core;

public interface ICurrentUser<TKey> where TKey : notnull
{
    bool IsAuthenticated { get; }
    string? Name { get; }
    bool TryGetId([MaybeNullWhen(false)] out TKey id);
}

public interface ICurrentUser : ICurrentUser<KeyType>;