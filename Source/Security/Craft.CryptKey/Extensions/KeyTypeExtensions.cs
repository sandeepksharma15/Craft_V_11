namespace Craft.CryptKey;

public static class KeyTypeExtensions
{
    extension<TKey>(TKey key) where TKey : struct
    {
        public string ToHashKey(IHashKeys hashKeys)
        {
            ArgumentNullException.ThrowIfNull(hashKeys);
            return hashKeys.Encode(key);
        }
    }

    extension(string? hashKey)
    {
        public KeyType ToKeyType(IHashKeys hashKeys) => hashKey.ToKeyType<KeyType>(hashKeys);

        public TKey ToKeyType<TKey>(IHashKeys hashKeys) where TKey : struct
        {
            ArgumentNullException.ThrowIfNull(hashKeys);
            return hashKeys.Decode<TKey>(hashKey);
        }

        public bool TryToKeyType(IHashKeys hashKeys, out KeyType key)
            => hashKey.TryToKeyType<KeyType>(hashKeys, out key);

        public bool TryToKeyType<TKey>(IHashKeys hashKeys, out TKey key) where TKey : struct
        {
            ArgumentNullException.ThrowIfNull(hashKeys);
            return hashKeys.TryDecode(hashKey, out key);
        }
    }
}