using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class OtherStringExtensions
{
    #region Private Fields

    private static readonly Regex PascalWordBoundaryRegex = new("([a-z0-9])([A-Z])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    #endregion Private Fields

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
                if (value[index] == character && ++count == occurrence)
                    return index;

            return -1;
        }

        public string? RemoveAll(params string[]? values)
        {
            if (value is null || values is null || values.Length == 0)
                return value;

            var result = value;

            foreach (var item in values)
                if (!string.IsNullOrEmpty(item))
                    result = result.Replace(item, string.Empty);

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
                if (!string.IsNullOrEmpty(postFix) && value.EndsWith(postFix, comparisonType))
                    return value[..^postFix.Length];

            return value;
        }

        public string? RemovePreFix(params string[]? preFixes)
            => value.RemovePreFix(StringComparison.Ordinal, preFixes);

        public string? RemovePreFix(StringComparison comparisonType, params string[]? preFixes)
        {
            if (string.IsNullOrEmpty(value) || preFixes is null || preFixes.Length == 0)
                return value;

            foreach (var preFix in preFixes)
                if (!string.IsNullOrEmpty(preFix) && value.StartsWith(preFix, comparisonType))
                    return value[preFix.Length..];

            return value;
        }

        public string? ReplaceFirst(string? search, string? replacement, StringComparison comparisonType = StringComparison.Ordinal)
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
        public string? Reverse(string? str)
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
        public string? ToCamelCase(string? str)
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
        public string? ToKebabCase(string? str)
        {
            if (string.IsNullOrEmpty(str))
                return str;
        }

        public string? Truncate(int maxLength)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

            var result = Regex.Replace(str, "([a-z0-9])([A-Z])", "$1-$2");
            return result.Replace(' ', '-').Replace('_', '-').ToLowerInvariant();
        }
    }
}
