using System.Text.RegularExpressions;
using Craft.Domain.Helpers;

namespace Craft.Domain.Tests.Helpers;

public class DomainConstantsTests
{

    [Theory]
    [InlineData("John", true)]
    [InlineData("John Doe", true)]
    [InlineData("John O'Brien", true)]
    [InlineData("John (Jr.)", true)]
    [InlineData("john", false)]
    [InlineData("123", false)]
    [InlineData("", false)]
    [InlineData("John1", false)]
    [InlineData("John@", false)]
    [InlineData("Émile", false)]
    [InlineData("J", true)]
    public void NameRegExpr_ShouldValidateCorrectly(string input, bool shouldMatch)
    {
        var regex = new Regex(DomainConstants.NameRegExpr, RegexOptions.None, TimeSpan.FromSeconds(1));

        var isMatch = regex.IsMatch(input);

        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("ABC", true)]
    [InlineData("XYZ", true)]
    [InlineData("AB1", false)]
    [InlineData("abc", false)]
    [InlineData("123", false)]
    [InlineData("", false)]
    [InlineData("ABc", false)]
    [InlineData("A B", false)]
    [InlineData("A", true)]
    public void AlphaCodeRegEx_ShouldValidateCorrectly(string input, bool shouldMatch)
    {
        var regex = new Regex(DomainConstants.AlphaCodeRegEx, RegexOptions.None, TimeSpan.FromSeconds(1));

        var isMatch = regex.IsMatch(input);

        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("A123", true)]
    [InlineData("ABC123", true)]
    [InlineData("A", true)]
    [InlineData("abc123", false)]
    [InlineData("123", false)]
    [InlineData("", false)]
    [InlineData("A1B2", true)]
    [InlineData("Ab1", false)]
    [InlineData("A-1", false)]
    public void AlphaNumCodeRegEx_ShouldValidateCorrectly(string input, bool shouldMatch)
    {
        var regex = new Regex(DomainConstants.AlphaNumCodeRegEx, RegexOptions.None, TimeSpan.FromSeconds(1));

        var isMatch = regex.IsMatch(input);

        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("123", true)]
    [InlineData("456789", true)]
    [InlineData("0", true)]
    [InlineData("12a", false)]
    [InlineData("abc", false)]
    [InlineData("", false)]
    [InlineData("000", true)]
    [InlineData("١٢", false)]
    [InlineData("-1", false)]
    public void DigitsRegEx_ShouldValidateCorrectly(string input, bool shouldMatch)
    {
        var regex = new Regex(DomainConstants.DigitsRegEx, RegexOptions.None, TimeSpan.FromSeconds(1));

        var isMatch = regex.IsMatch(input);

        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("123", true)]
    [InlineData("9", true)]
    [InlineData("0", false)]
    [InlineData("012", false)]
    [InlineData("", false)]
    [InlineData("10", true)]
    [InlineData("1010", true)]
    [InlineData("100000", true)]
    [InlineData("01", false)]
    [InlineData("1a", false)]
    public void DigitsGreaterThanZero_ShouldValidateCorrectly(string input, bool shouldMatch)
    {
        var regex = new Regex(DomainConstants.DigitsGreaterThanZero, RegexOptions.None, TimeSpan.FromSeconds(1));

        var isMatch = regex.IsMatch(input);

        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("123", true)]
    [InlineData("123.45", true)]
    [InlineData(".45", true)]
    [InlineData("0.5", true)]
    [InlineData("abc", false)]
    [InlineData("", false)]
    [InlineData("10.01", true)]
    [InlineData("00.50", true)]
    [InlineData("abc123", false)]
    [InlineData("123abc", false)]
    [InlineData("1.2.3", false)]
    [InlineData("5.", false)]
    [InlineData(".", false)]
    [InlineData("-1", false)]
    [InlineData("+1", false)]
    [InlineData("1e3", false)]
    [InlineData("1,5", false)]
    public void NumberRegEx_ShouldValidateCorrectly(string input, bool shouldMatch)
    {
        var regex = new Regex(DomainConstants.NumberRegEx, RegexOptions.None, TimeSpan.FromSeconds(1));

        var isMatch = regex.IsMatch(input);

        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("user.name@domain.co.uk", true)]
    [InlineData("user+tag@example.com", true)]
    [InlineData("invalid@", false)]
    [InlineData("@example.com", false)]
    [InlineData("notanemail", false)]
    [InlineData("", false)]
    [InlineData("a@localhost", true)]
    [InlineData("a@-host", false)]
    [InlineData("a@host-", false)]
    [InlineData("a b@example.com", false)]
    public void EmailRegExpr_ShouldValidateCorrectly(string input, bool shouldMatch)
    {
        var regex = new Regex(DomainConstants.EmailRegExpr, RegexOptions.None, TimeSpan.FromSeconds(1));

        var isMatch = regex.IsMatch(input);

        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("+1234567890", true)]
    [InlineData("123-456-7890", true)]
    [InlineData("(123) 456-7890", true)]
    [InlineData("+1 (234) 567-8900", true)]
    [InlineData("abc", false)]
    [InlineData("", false)]
    [InlineData("---", false)]
    [InlineData("( )", false)]
    [InlineData("+", false)]
    [InlineData(" ", false)]
    [InlineData("1\t2", false)]
    [InlineData("1\n2", false)]
    [InlineData("١٢٣", true)]
    [InlineData("1+2", false)]
    public void PhoneRegExpr_ShouldValidateCorrectly(string input, bool shouldMatch)
    {
        var regex = new Regex(DomainConstants.PhoneRegExpr, RegexOptions.None, TimeSpan.FromSeconds(1));

        var isMatch = regex.IsMatch(input);

        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("http://example.com", true)]
    [InlineData("https://example.com", true)]
    [InlineData("https://example.com/path", true)]
    [InlineData("https://example.com/path?query=value", true)]
    [InlineData("ftp://example.com", false)]
    [InlineData("example.com", false)]
    [InlineData("", false)]
    [InlineData("http://", false)]
    [InlineData("https://a b", false)]
    [InlineData("HTTPS://example.com", false)]
    public void UrlRegExpr_ShouldValidateCorrectly(string input, bool shouldMatch)
    {
        var regex = new Regex(DomainConstants.UrlRegExpr, RegexOptions.None, TimeSpan.FromSeconds(1));

        var isMatch = regex.IsMatch(input);

        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("ABC123-456", true)]
    [InlineData("XYZ", true)]
    [InlineData("123", true)]
    [InlineData("A1-B2", true)]
    [InlineData("abc", false)]
    [InlineData("", false)]
    [InlineData("1A-2B", true)]
    [InlineData("-", true)]
    [InlineData("A--B", true)]
    [InlineData("A_b", false)]
    [InlineData("A1b", false)]
    public void AlphaNumCodeWithHyphenRegEx_ShouldValidateCorrectly(string input, bool shouldMatch)
    {
        var regex = new Regex(DomainConstants.AlphaNumCodeWithHyphenRegEx, RegexOptions.None, TimeSpan.FromSeconds(1));

        var isMatch = regex.IsMatch(input);

        Assert.Equal(shouldMatch, isMatch);
    }

    public static TheoryData<string, string> Patterns => new()
    {
        { DomainConstants.NameRegExpr, "John" },
        { DomainConstants.AlphaCodeRegEx, "ABC" },
        { DomainConstants.AlphaNumCodeRegEx, "A1" },
        { DomainConstants.DigitsRegEx, "123" },
        { DomainConstants.DigitsGreaterThanZero, "101" },
        { DomainConstants.NumberRegEx, "12.5" },
        { DomainConstants.AlphaNumCodeWithHyphenRegEx, "A1-B2" },
        { DomainConstants.EmailRegExpr, "a@example.com" },
        { DomainConstants.PhoneRegExpr, "+1 (234) 567-8900" },
        { DomainConstants.UrlRegExpr, "https://example.com/path" }
    };

    [Theory]
    [MemberData(nameof(Patterns))]
    public void Patterns_RejectPartialAndMultilineMatches(string pattern, string valid)
    {
        foreach (var options in new[] { RegexOptions.None, RegexOptions.Multiline, RegexOptions.NonBacktracking })
        {
            var regex = new Regex(pattern, options, TimeSpan.FromSeconds(1));
            var match = regex.Match(valid);
            Assert.True(match.Success);
            Assert.Equal(valid, match.Value);
            Assert.DoesNotMatch(regex, string.Empty);
            foreach (var invalid in new[] { "\0" + valid, valid + "\n", valid + "\r\n", "\n" + valid,
                "!\n" + valid + "\n!", valid + "\0", valid + "\u2028" })
                Assert.False(regex.IsMatch(invalid), $"Pattern {pattern} accepted {System.Text.Json.JsonSerializer.Serialize(invalid)} with {options}");
        }
    }

    [Theory]
    [MemberData(nameof(Patterns))]
    public void Patterns_WorkWithDataAnnotations(string pattern, string valid)
    {
        var attribute = new System.ComponentModel.DataAnnotations.RegularExpressionAttribute(pattern)
        {
            MatchTimeoutInMilliseconds = 1000
        };
        Assert.True(attribute.IsValid(valid));
        Assert.False(attribute.IsValid(valid + "\n"));
        Assert.True(attribute.IsValid(null));
        Assert.True(attribute.IsValid(string.Empty));
    }
}
