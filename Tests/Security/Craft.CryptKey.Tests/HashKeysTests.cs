using System.Globalization;
using Craft.CryptKey;
using HashidsNet;
using Microsoft.Extensions.Options;

namespace Craft.CryptKey.Tests;

public class HashKeysTests
{
    internal static HashKeyOptions Configuration() => new() { Salt = "test-application" };
    private static HashKeys Create() => new(Configuration());
    private static string Raw(params long[] values)
    {
        var options = Configuration();
        return new Hashids(options.Salt, options.MinHashLength, options.Alphabet, options.Separators).EncodeLong(values);
    }

    [Fact]
    public void IntegerKeys_RoundTripAcrossTheirFullRanges()
    {
        var keys = Create();
        foreach (var key in new[] { int.MinValue, int.MinValue + 1, -1, 0, 1, int.MaxValue })
            RoundTrip(keys, key);
        foreach (var key in new[] { long.MinValue, long.MinValue + 1, -1, 0, 1, (long)int.MaxValue + 1, long.MaxValue })
            RoundTrip(keys, key);
    }

    [Fact]
    public void GuidKeys_RoundTripIncludingEmptyAndEveryBitSet()
    {
        var keys = Create();
        foreach (var key in new[] { Guid.Empty, Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"), Guid.NewGuid() })
            RoundTrip(keys, key);
    }

    private static void RoundTrip<TKey>(IHashKeys keys, TKey key) where TKey : struct
    {
        var token = keys.Encode(key);
        Assert.Matches("\\A[A-Za-z0-9_-]+\\z", token);
        Assert.True(token.Length >= 10);
        Assert.Equal(key, keys.Decode<TKey>(token));
        Assert.True(keys.TryDecode<TKey>(token, out var decoded));
        Assert.Equal(key, decoded);
        Assert.Equal(token, Create().Encode(key));
    }

    [Fact]
    public void DifferentKeysAndTypes_HaveDistinctTokens()
    {
        var keys = Create();
        var tokens = Enumerable.Range(-500, 1001).SelectMany(i => new[] { keys.Encode(i), keys.Encode((long)i) }).ToArray();
        Assert.Equal(tokens.Length, tokens.Distinct().Count());
        Assert.False(keys.TryDecode<int>(keys.Encode(42L), out _));
        Assert.False(keys.TryDecode<long>(keys.Encode(42), out _));
        Assert.False(keys.TryDecode<Guid>(keys.Encode(42L), out _));
    }

    [Fact]
    public void Configuration_IsSnapshottedAtConstruction()
    {
        var options = Configuration();
        var keys = new HashKeys(options);
        var token = keys.Encode(42L);
        options.Salt = "different";
        options.Alphabet = "";
        options.Separators = "";
        options.MinHashLength = 0;
        options.MaxHashLength = 1;
        Assert.Equal(token, keys.Encode(42L));
        Assert.Equal(42L, keys.Decode<long>(token));
    }

    [Fact]
    public void DifferentConfiguration_DoesNotDecodeTheOriginalToken()
    {
        var options = Configuration();
        options.Salt = "another-application";
        var other = new HashKeys(options);
        var token = Create().Encode(123L);
        Assert.NotEqual(token, other.Encode(123L));
        Assert.False(other.TryDecode<long>(token, out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not/a/token")]
    [InlineData("a")]
    [InlineData("abc")]
    public void InvalidTokens_ReturnFalseOrThrowFormatException(string? token)
    {
        var keys = Create();
        Assert.False(keys.TryDecode<long>(token, out var value));
        Assert.Equal(0L, value);
        Assert.Throws<FormatException>(() => keys.Decode<long>(token!));
    }

    [Fact]
    public void TokenLength_IsBoundedOnBothPaths()
    {
        var keys = Create();
        Assert.False(keys.TryDecode<long>(new string('a', Configuration().MaxHashLength + 1), out _));
        var options = Configuration();
        options.MinHashLength = 0;
        options.MaxHashLength = 1;
        Assert.Throws<InvalidOperationException>(() => new HashKeys(options).Encode(long.MaxValue));
        options.MinHashLength = 64;
        options.MaxHashLength = 64;
        keys = new HashKeys(options);
        var token = keys.Encode(1L);
        Assert.Equal(64, token.Length);
        Assert.Equal(1L, keys.Decode<long>(token));
    }

    [Fact]
    public void MalformedTypedPayloads_AreRejected()
    {
        var keys = Create();
        long[][] invalidIntegers = [[], [1], [1, 0], [1, 0, 0, 0], [9, 0, 0],
            [1, (long)int.MaxValue + 1, 0], [1, 1, 2]];
        foreach (var values in invalidIntegers)
            Assert.False(keys.TryDecode<int>(Raw(values), out _));
        long[][] invalidLongs = [[2], [2, 1], [2, 0, 0, 0], [9, 0, 0], [2, 1, 2]];
        foreach (var values in invalidLongs)
            Assert.False(keys.TryDecode<long>(Raw(values), out _));
        long[][] invalidGuids = [[3], [3, 0, 0, 0], [3, 0, 0, 0, 0, 0], [9, 0, 0, 0, 0],
            [3, (long)uint.MaxValue + 1, 0, 0, 0], [3, 0, (long)uint.MaxValue + 1, 0, 0],
            [3, 0, 0, (long)uint.MaxValue + 1, 0], [3, 0, 0, 0, (long)uint.MaxValue + 1]];
        foreach (var values in invalidGuids)
            Assert.False(keys.TryDecode<Guid>(Raw(values), out _));
        Assert.False(keys.TryDecode<long>(Raw(42), out _));
    }

    [Fact]
    public void UnsupportedTypes_ThrowInsteadOfSilentlyConverting()
    {
        var keys = Create();
        Assert.Throws<NotSupportedException>(() => keys.Encode(1m));
        Assert.Throws<NotSupportedException>(() => keys.TryDecode<decimal>(null, out _));
        Assert.Throws<NotSupportedException>(() => keys.Decode<ulong>(keys.Encode(1L)));
    }

    [Fact]
    public void MalformedInput_DoesNotEscapeTryDecode()
    {
        var keys = Create();
        var random = new Random(937);
        var alphabet = Configuration().Alphabet;
        foreach (var character in alphabet)
            keys.TryDecode<long>(character.ToString(), out _);
        for (var i = 0; i < 2000; i++)
        {
            var token = new string(Enumerable.Range(0, random.Next(1, 257)).Select(_ => alphabet[random.Next(alphabet.Length)]).ToArray());
            if (keys.TryDecode<long>(token, out var value))
                Assert.Equal(token, keys.Encode(value));
        }
    }

    [Fact]
    public async Task Singleton_IsDeterministicAcrossConcurrentCultures()
    {
        var keys = Create();
        var expected = keys.Encode(-987654321L);
        var tasks = Enumerable.Range(0, 64).Select(async i =>
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(i % 2 == 0 ? "ar-SA" : "fr-FR");
            await Task.Yield();
            Assert.Equal(expected, keys.Encode(-987654321L));
            Assert.Equal(-987654321L, keys.Decode<long>(expected));
        });
        await Task.WhenAll(tasks);
    }
}