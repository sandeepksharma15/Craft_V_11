#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Text.RegularExpressions;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class RegexExtensions
{
    extension(Regex? regex)
    {
        public Match RegexMatch(string? source)
        {
            if (regex is null || source is null)
                return Match.Empty;

            var match = regex.Match(source);
            return match.Success ? match : Match.Empty;
        }
    }

    extension(string? source)
    {
        public Match RegexMatch(Regex? regex)
            => regex.RegexMatch(source);
    }
}
