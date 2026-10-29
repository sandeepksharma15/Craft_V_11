using Craft.CryptKey.Encoding;

namespace Craft.CryptKey.Tests;

public class StableTokenTests
{
    #region Public Methods

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", "pbUltYtmtj")]
    [InlineData("00112233-4455-6677-8899-aabbccddeeff", "yYUnjykI3k2OZESgjD42EUv7V5wX")]
    [InlineData("ffffffff-ffff-ffff-ffff-ffffffffffff", "nnUZ134DaU0PEmZRUL26o01fvoGMLX")]
    public void GuidTokens_KeepTheirPersistedRepresentation(string value, string token)
    {
        var keys = new HashKeys(HashKeysTests.Configuration());
        var key = Guid.Parse(value);
        Assert.Equal(token, keys.Encode(key));
        Assert.Equal(key, keys.Decode<Guid>(token));
    }

    [Theory]
    [InlineData(-2147483648, "JdcNODDAZT8")]
    [InlineData(-1, "yzEkc6tNKQ")]
    [InlineData(0, "Eemyckt5e4")]
    [InlineData(1, "yzEkc9c5KQ")]
    [InlineData(2147483647, "b7cPl88X9s3")]
    public void IntTokens_KeepTheirPersistedRepresentation(int value, string token)
    {
        var keys = new HashKeys(HashKeysTests.Configuration());
        var key = value;
        Assert.Equal(token, keys.Encode(key));
        Assert.Equal(key, keys.Decode<int>(token));
    }

    [Theory]
    [InlineData(-9223372036854775808L, "APF2Pm5J70RDw72H3")]
    [InlineData(-1L, "7zpwFltPeq")]
    [InlineData(0L, "yzEVF6t5WQ")]
    [InlineData(1L, "7zpwFQcYeq")]
    [InlineData(9223372036854775807L, "pwFA2MPL79ogr7AfY")]
    public void LongTokens_KeepTheirPersistedRepresentation(long value, string token)
    {
        var keys = new HashKeys(HashKeysTests.Configuration());
        var key = value;
        Assert.Equal(token, keys.Encode(key));
        Assert.Equal(key, keys.Decode<long>(token));
    }

    #endregion Public Methods
}
