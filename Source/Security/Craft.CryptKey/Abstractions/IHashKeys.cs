namespace Craft.CryptKey.Abstractions;

public interface IHashKeys
{
    #region Public Methods

    TKey Decode<TKey>(string? hashKey) where TKey : struct;

    string Encode<TKey>(TKey key) where TKey : struct;

    bool TryDecode<TKey>(string? hashKey, out TKey key) where TKey : struct;

    #endregion Public Methods
}
