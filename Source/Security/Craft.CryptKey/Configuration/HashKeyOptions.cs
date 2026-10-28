using HashidsNet;

namespace Craft.CryptKey;

public sealed class HashKeyOptions
{
    public string Salt { get; set; } = string.Empty;
    public string Alphabet { get; set; } = Hashids.DEFAULT_ALPHABET;
    public string Separators { get; set; } = Hashids.DEFAULT_SEPS;
    public int MinHashLength { get; set; } = 10;
    public int MaxHashLength { get; set; } = 256;
}