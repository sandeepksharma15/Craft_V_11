#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class StandardStringExtensions
{
    extension(string? value)
    {
        public string? EnsureStartsWith(
            char character,
            StringComparison comparisonType = StringComparison.Ordinal)
            => value is null
                ? null
                : value.StartsWith(character.ToString(), comparisonType)
                    ? value
                    : character + value;

        public string? EnsureEndsWith(
            char character,
            StringComparison comparisonType = StringComparison.Ordinal)
            => value is null
                ? null
                : value.EndsWith(character.ToString(), comparisonType)
                    ? value
                    : value + character;

        public string? FirstCharToUpper()
            => string.IsNullOrEmpty(value)
                ? value
                : char.ToUpperInvariant(value[0]) + value[1..];

        public string? GetStringAfterLastDelimiter(char delimiter = '.')
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var index = value.LastIndexOf(delimiter);
            return index < 0 ? value : value[(index + 1)..];
        }

        public bool IsNullOrEmpty()
            => string.IsNullOrEmpty(value);

        public bool IsNullOrWhiteSpace()
            => string.IsNullOrWhiteSpace(value);

        public string? Left(int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(length);

            return string.IsNullOrEmpty(value) || length >= value.Length
                ? value
                : value[..length];
        }

        public string? Right(int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(length);

            return string.IsNullOrEmpty(value) || length >= value.Length
                ? value
                : value[^length..];
        }
    }
}
