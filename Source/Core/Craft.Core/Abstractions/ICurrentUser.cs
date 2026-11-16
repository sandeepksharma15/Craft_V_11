using System.Diagnostics.CodeAnalysis;

namespace Craft.Core.Abstractions;

public interface ICurrentUser<TKey> where TKey : notnull
{
    #region Public Properties

    bool IsAuthenticated { get; }
    string? Name { get; }

    #endregion Public Properties

    #region Public Methods

    bool TryGetId([MaybeNullWhen(false)] out TKey id);

    #endregion Public Methods
}

public interface ICurrentUser : ICurrentUser<KeyType>;
