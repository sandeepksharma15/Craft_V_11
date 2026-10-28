using Craft.CryptKey;

namespace Craft.CryptKey.Tests;

public class KeyTypeExtensionsTests
{
    private readonly IHashKeys _keys = new HashKeys(HashKeysTests.Configuration());

    [Fact]
    public void DefaultKeyType_RoundTrips()
    {
        KeyType key = default;
        var token = key.ToHashKey(_keys);
        Assert.Equal(key, token.ToKeyType(_keys));
        Assert.True(token.TryToKeyType(_keys, out var result));
        Assert.Equal(key, result);
        Assert.False("!".TryToKeyType(_keys, out _));
    }

    [Fact]
    public void GenericKeys_RoundTrip()
    {
        Assert.Equal(-42, (-42).ToHashKey(_keys).ToKeyType<int>(_keys));
        Assert.Equal(long.MinValue, long.MinValue.ToHashKey(_keys).ToKeyType<long>(_keys));
        var key = Guid.NewGuid();
        var token = key.ToHashKey(_keys);
        Assert.Equal(key, token.ToKeyType<Guid>(_keys));
        Assert.True(token.TryToKeyType<Guid>(_keys, out var result));
        Assert.Equal(key, result);
        Assert.False("!".TryToKeyType<Guid>(_keys, out result));
        Assert.Equal(Guid.Empty, result);
    }

    [Fact]
    public void NullService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => 1.ToHashKey(null!));
        Assert.Throws<ArgumentNullException>(() => "token".ToKeyType(null!));
        Assert.Throws<ArgumentNullException>(() => "token".ToKeyType<int>(null!));
        Assert.Throws<ArgumentNullException>(() => "token".TryToKeyType(null!, out _));
        Assert.Throws<ArgumentNullException>(() => "token".TryToKeyType<int>(null!, out _));
    }

    [Fact]
    public void NullAndMalformedTokens_FollowTheServiceContract()
    {
        string? token = null;
        Assert.False(token.TryToKeyType(_keys, out _));
        Assert.Throws<FormatException>(() => token.ToKeyType(_keys));
        Assert.Throws<FormatException>(() => "!".ToKeyType<int>(_keys));
    }
}