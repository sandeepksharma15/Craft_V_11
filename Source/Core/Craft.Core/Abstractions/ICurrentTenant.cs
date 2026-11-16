using System.Diagnostics.CodeAnalysis;

namespace Craft.Core.Abstractions;

public interface ICurrentTenant<TKey> where TKey : notnull
{
    #region Public Properties

    string? Identifier { get; }
    bool IsAvailable { get; }

    #endregion Public Properties

    #region Public Methods

    bool TryGetId([MaybeNullWhen(false)] out TKey id);

    #endregion Public Methods
}

public interface ICurrentTenant : ICurrentTenant<KeyType>;
