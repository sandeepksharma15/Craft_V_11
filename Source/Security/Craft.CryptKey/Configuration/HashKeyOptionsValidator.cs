using HashidsNet;
using Microsoft.Extensions.Options;

namespace Craft.CryptKey;

internal sealed class HashKeyOptionsValidator : IValidateOptions<HashKeyOptions>
{
    public ValidateOptionsResult Validate(string? name, HashKeyOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Salt))
            return ValidateOptionsResult.Fail("Salt must be explicitly configured.");
        if (options.Salt != options.Salt.Trim())
            return ValidateOptionsResult.Fail("Salt must not have leading or trailing whitespace.");
        if (options.MinHashLength < 0 || options.MaxHashLength < 1 || options.MinHashLength > options.MaxHashLength)
            return ValidateOptionsResult.Fail("Lengths must satisfy 0 <= MinHashLength <= MaxHashLength and MaxHashLength > 0.");
        if (string.IsNullOrEmpty(options.Alphabet) || options.Alphabet.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_'))
            return ValidateOptionsResult.Fail("Alphabet must contain only URL-safe ASCII letters, digits, hyphens or underscores.");
        if (options.Alphabet.Distinct().Count() != options.Alphabet.Length)
            return ValidateOptionsResult.Fail("Alphabet must not contain duplicate characters.");
        if (string.IsNullOrEmpty(options.Separators) || options.Separators.Any(c => !options.Alphabet.Contains(c)))
            return ValidateOptionsResult.Fail("Separators must be nonempty and contained in Alphabet.");
        if (options.Separators.Distinct().Count() != options.Separators.Length)
            return ValidateOptionsResult.Fail("Separators must not contain duplicate characters.");
        try
        {
            _ = new Hashids(options.Salt, options.MinHashLength, options.Alphabet, options.Separators);
        }
        catch (ArgumentException exception)
        {
            return ValidateOptionsResult.Fail(exception.Message);
        }
        return ValidateOptionsResult.Success;
    }
}