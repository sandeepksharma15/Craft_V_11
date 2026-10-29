using Craft.CryptKey.Abstractions;
using Craft.CryptKey.Configuration;
using HashidsNet;
using Microsoft.Extensions.Options;

namespace Craft.CryptKey.Encoding;

public sealed class HashKeys : IHashKeys
{
    #region Private Fields

    private readonly string _alphabet;
    private readonly Hashids _encoder;
    private readonly int _maxHashLength;

    #endregion Private Fields

    #region Public Constructors

    public HashKeys(HashKeyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var validation = new HashKeyOptionsValidator().Validate(Options.DefaultName, options);

        if (validation.Failed)
            throw new OptionsValidationException(Options.DefaultName, typeof(HashKeyOptions), validation.Failures);

        _encoder = new Hashids(options.Salt, options.MinHashLength, options.Alphabet, options.Separators);
        _alphabet = options.Alphabet;
        _maxHashLength = options.MaxHashLength;
    }

    #endregion Public Constructors

    #region Public Methods

    public TKey Decode<TKey>(string? hashKey) where TKey : struct
        => TryDecode(hashKey, out TKey key)
                ? key
                : throw new FormatException("The token is not a valid key of the requested type for this configuration.");

    public string Encode<TKey>(TKey key) where TKey : struct
    {
        var hash = _encoder.EncodeLong(KeyPayload.Encode(key));

        if (hash.Length > _maxHashLength)
            throw new InvalidOperationException("Encoded key exceeds MaxHashLength. Increase the configured limit.");

        return hash;
    }

    public bool TryDecode<TKey>(string? hashKey, out TKey key) where TKey : struct
    {
        KeyPayload.EnsureSupported<TKey>();
        key = default;

        if (string.IsNullOrEmpty(hashKey) || hashKey.Length > _maxHashLength || hashKey.Any(c => !_alphabet.Contains(c)))
            return false;

        var values = _encoder.DecodeLong(hashKey);

        return KeyPayload.TryDecode(values, out key);
    }

    #endregion Public Methods
}
