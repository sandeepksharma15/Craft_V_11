#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Text.RegularExpressions;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class RegexExtensions
{
    public static Match RegexMatch(this Regex regex, string source)
    {
        if ((regex == null) || (source == null))
            return Match.Empty;

        var match = regex.Match(source);

        return match.Success ? match : Match.Empty;
    }

    public static Match RegexMatch(this string? source, Regex? regex) =>
        regex!.RegexMatch(source!);
}
