using HashidsNet;

namespace Craft.CryptKey.Configuration;

public sealed class HashKeyOptions
{
    #region Public Properties

    public string Alphabet { get; set; } = Hashids.DEFAULT_ALPHABET;
    public int MaxHashLength { get; set; } = 256;
    public int MinHashLength { get; set; } = 10;
    public string Salt { get; set; } = string.Empty;
    public string Separators { get; set; } = Hashids.DEFAULT_SEPS;

    #endregion Public Properties
}
