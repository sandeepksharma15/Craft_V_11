using System.Buffers.Binary;

namespace Craft.CryptKey.Encoding;

internal static class KeyPayload
{
    #region Private Fields

    private const long GuidTag = 3;

    // These discriminators define the persisted v1 wire format, not application configuration.
    private const long Int32Tag = 1;

    private const long Int64Tag = 2;

    #endregion Private Fields

    #region Private Methods

    private static long[] EncodeInteger(long value, long tag)
        => value < 0 ? [tag, ~value, 1] : [tag, value, 0];

    #endregion Private Methods

    #region Internal Methods

    internal static long[] Encode<TKey>(TKey key) where TKey : struct
    {
        EnsureSupported<TKey>();
        if (typeof(TKey) == typeof(int))
            return EncodeInteger((int)(object)key, Int32Tag);
        if (typeof(TKey) == typeof(long))
            return EncodeInteger((long)(object)key, Int64Tag);

        Span<byte> bytes = stackalloc byte[16];
        ((Guid)(object)key).TryWriteBytes(bytes, bigEndian: true, out _);
        return [GuidTag, BinaryPrimitives.ReadUInt32BigEndian(bytes),
            BinaryPrimitives.ReadUInt32BigEndian(bytes[4..]),
            BinaryPrimitives.ReadUInt32BigEndian(bytes[8..]),
            BinaryPrimitives.ReadUInt32BigEndian(bytes[12..])];
    }

    internal static void EnsureSupported<TKey>() where TKey : struct
    {
        if (typeof(TKey) != typeof(int) && typeof(TKey) != typeof(long) && typeof(TKey) != typeof(Guid))
            throw new NotSupportedException($"Key type '{typeof(TKey)}' is not supported. Use int, long or Guid.");
    }

    internal static bool TryDecode<TKey>(ReadOnlySpan<long> values, out TKey key) where TKey : struct
    {
        key = default;
        if (typeof(TKey) == typeof(int))
        {
            if (values is not [Int32Tag, <= int.MaxValue, 0 or 1])
                return false;
            key = (TKey)(object)(int)(values[2] == 0 ? values[1] : ~values[1]);
            return true;
        }
        if (typeof(TKey) == typeof(long))
        {
            if (values is not [Int64Tag, _, 0 or 1])
                return false;
            key = (TKey)(object)(values[2] == 0 ? values[1] : ~values[1]);
            return true;
        }
        if (values is not [GuidTag, <= uint.MaxValue, <= uint.MaxValue,
            <= uint.MaxValue, <= uint.MaxValue])
            return false;

        Span<byte> bytes = stackalloc byte[16];
        for (var i = 0; i < 4; i++)
            BinaryPrimitives.WriteUInt32BigEndian(bytes[(i * 4)..], (uint)values[i + 1]);
        key = (TKey)(object)new Guid(bytes, bigEndian: true);
        return true;
    }

    #endregion Internal Methods
}
