using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class OtherStringExtensions
{
    private static readonly Regex PascalWordBoundaryRegex = new(
        "([a-z0-9])([A-Z])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    extension(string? value)
    {
        public string? NormalizeLineEndings()
            => value?
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace("\n", Environment.NewLine);

        public int NthIndexOf(char character, int occurrence)
        {
            if (value is null || occurrence <= 0)
                return -1;

            var count = 0;

            for (var index = 0; index < value.Length; index++)
            {
                if (value[index] == character && ++count == occurrence)
                    return index;
            }

            return -1;
        }

        public string? RemoveAll(params string[]? values)
        {
            if (value is null || values is null || values.Length == 0)
                return value;

            var result = value;

            foreach (var item in values)
            {
                if (!string.IsNullOrEmpty(item))
                    result = result.Replace(item, string.Empty);
            }

            return result;
        }

        public string? RemoveExtraSpaces()
            => string.IsNullOrEmpty(value)
                ? value
                : string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        public string? RemovePostFix(params string[]? postFixes)
            => value.RemovePostFix(StringComparison.Ordinal, postFixes);

        public string? RemovePostFix(StringComparison comparisonType, params string[]? postFixes)
        {
            if (string.IsNullOrEmpty(value) || postFixes is null || postFixes.Length == 0)
                return value;

            foreach (var postFix in postFixes)
            {
                if (!string.IsNullOrEmpty(postFix) && value.EndsWith(postFix, comparisonType))
                    return value[..^postFix.Length];
            }

            return value;
        }

        public string? RemovePreFix(params string[]? preFixes)
            => value.RemovePreFix(StringComparison.Ordinal, preFixes);

        public string? RemovePreFix(StringComparison comparisonType, params string[]? preFixes)
        {
            if (string.IsNullOrEmpty(value) || preFixes is null || preFixes.Length == 0)
                return value;

            foreach (var preFix in preFixes)
            {
                if (!string.IsNullOrEmpty(preFix) && value.StartsWith(preFix, comparisonType))
                    return value[preFix.Length..];
            }

            return value;
        }

        public string? ReplaceFirst(
            string? search,
            string? replacement,
            StringComparison comparisonType = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(search) || replacement is null)
                return value;

            var index = value.IndexOf(search, comparisonType);

            return index < 0
                ? value
                : string.Concat(value.AsSpan(0, index), replacement, value.AsSpan(index + search.Length));
        }

    /// <summary>
    /// Reverses the characters in the string.
    /// </summary>
    public static string? Reverse(this string? str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        var charArray = str.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }
        public string ToSha256()
        {
            ArgumentNullException.ThrowIfNull(value);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        }

    /// <summary>
    /// Converts a string to camelCase (e.g., "hello world" -> "helloWorld").
    /// </summary>
    public static string? ToCamelCase(this string? str)
    {
        var pascal = str.ToPascalCase();

        return string.IsNullOrEmpty(pascal)
            ? pascal
            : char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }
        public bool IsBase64String()
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length % 4 != 0)
                return false;

            var buffer = new byte[value.Length / 4 * 3];
            return Convert.TryFromBase64String(value, buffer, out _);
        }

    /// <summary>
    /// Converts a string to kebab-case (e.g., "HelloWorld" -> "hello-world").
    /// </summary>
    public static string? ToKebabCase(this string? str)
    {
        if (string.IsNullOrEmpty(str))
            return str;
        public string? Truncate(int maxLength)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        var result = Regex.Replace(str, "([a-z0-9])([A-Z])", "$1-$2");
        return result.Replace(' ', '-').Replace('_', '-').ToLowerInvariant();
    }
            return string.IsNullOrEmpty(value) || value.Length <= maxLength
                ? value
                : value[..maxLength];
        }

    /// <summary>
    /// Computes the MD5 hash of the current string and returns it as a hexadecimal string.
    /// </summary>
    public static string ToMd5(this string str)
    {
        ArgumentNullException.ThrowIfNull(str);

        byte[] inputBytes = Encoding.UTF8.GetBytes(str);
        byte[] hashBytes = MD5.HashData(inputBytes);

        StringBuilder sb = new(hashBytes.Length * 2);

        foreach (byte hashByte in hashBytes)
            sb.Append(hashByte.ToString("X2"));
        public string? TruncateWithEllipsis(int maxLength, string ellipsis = "...")
        {
            ArgumentOutOfRangeException.ThrowIfNegative(maxLength);
            ArgumentNullException.ThrowIfNull(ellipsis);

            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;

        return sb.ToString();
    }
            return maxLength <= ellipsis.Length
                ? value[..maxLength]
                : value[..(maxLength - ellipsis.Length)] + ellipsis;
        }

        public string? ToPascalCase()
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var words = value.Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries);
            var result = new StringBuilder();

            foreach (var word in words)
                result.Append(char.ToUpperInvariant(word[0])).Append(word[1..].ToLowerInvariant());

            return result.ToString();
        }

    /// <summary>
    /// Converts a string to snake_case (e.g., "HelloWorld" -> "hello_world").
    /// </summary>
    public static string? ToSnakeCase(this string? str)
    {
        if (string.IsNullOrEmpty(str))
            return str;
        public string? ToCamelCase()
        {
            var pascal = value.ToPascalCase();

            return string.IsNullOrEmpty(pascal)
                ? pascal
                : char.ToLowerInvariant(pascal[0]) + pascal[1..];
        }

        public string? ToSnakeCase()
            => ToSeparatedCase(value, '_');

        public string? ToKebabCase()
            => ToSeparatedCase(value, '-');

        public string? Reverse()
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var textElements = StringInfo.ParseCombiningCharacters(value);
            var result = new StringBuilder(value.Length);

    /// <summary>
    /// Truncates the string to the specified maximum length.
    /// </summary>
    public static string? Truncate(this string? str, int maxLength)
    {
        if (string.IsNullOrEmpty(str) || str.Length <= maxLength)
            return str;
            for (var index = textElements.Length - 1; index >= 0; index--)
            {
                var start = textElements[index];
                var length = index == textElements.Length - 1
                    ? value.Length - start
                    : textElements[index + 1] - start;

        return str[..maxLength];
    }
                result.Append(value.AsSpan(start, length));
            }

            return result.ToString();
        }

        public int CountOccurrences(
            string? substring,
            StringComparison comparisonType = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(substring))
                return 0;

            var count = 0;
            var index = 0;

            while ((index = value.IndexOf(substring, index, comparisonType)) >= 0)
            {
                count++;
                index += substring.Length;
            }

            return count;
        }

        public bool IsNumeric()
            => !string.IsNullOrEmpty(value) && value.All(char.IsDigit);

        public bool IsAlphabetic()
            => !string.IsNullOrEmpty(value) && value.All(char.IsLetter);

        public bool IsAlphanumeric()
            => !string.IsNullOrEmpty(value) && value.All(char.IsLetterOrDigit);
    }

    private static string? ToSeparatedCase(string? value, char separator)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var separated = PascalWordBoundaryRegex.Replace(value, "$1" + separator + "$2");

        foreach (var character in new[] { ' ', '_', '-' })
            separated = separated.Replace(character, separator);

        return separated.ToLowerInvariant();
    }
}
