namespace Craft.CryptKey;

public interface IHashKeys
{
    string Encode<TKey>(TKey key) where TKey : struct;
    TKey Decode<TKey>(string? hashKey) where TKey : struct;
    bool TryDecode<TKey>(string? hashKey, out TKey key) where TKey : struct;
}