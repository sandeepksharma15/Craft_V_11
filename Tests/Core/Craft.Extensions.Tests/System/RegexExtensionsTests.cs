using System.Text.RegularExpressions;

namespace Craft.Extensions.Tests.System;

public class RegexExtensionsTests
{
    [Theory]
    [InlineData("Test123", @"\d+", true)]
    [InlineData("NoDigits", @"\d+", false)]
    [InlineData("", @"\d+", false)]
    [InlineData(null, @"\d+", false)]
    public void RegexMatch_FromRegex_ShouldReturnExpectedMatch(
        string? source,
        string pattern,
        bool expectedSuccess)
    {
        var regex = new Regex(pattern);

        var result = regex.RegexMatch(source);

        Assert.NotNull(result);
        Assert.Equal(expectedSuccess, result.Success);
    }

    [Fact]
    public void RegexMatch_FromRegex_ShouldPreserveMatchDetails()
    {
        var regex = new Regex(@"(?<name>[A-Za-z]+)(?<number>\d+)");

        var result = regex.RegexMatch("Test123");

        Assert.True(result.Success);
        Assert.Equal("Test123", result.Value);
        Assert.Equal("Test", result.Groups["name"].Value);
        Assert.Equal("123", result.Groups["number"].Value);
    }

    [Fact]
    public void RegexMatch_FromRegex_ShouldReturnEmptyMatchWhenRegexIsNull()
    {
        Regex? regex = null;

        var result = regex.RegexMatch("Test123");

        Assert.Same(Match.Empty, result);
    }

    [Theory]
    [InlineData("Input123", "Input\\d+", true)]
    [InlineData("Test123", "\\d{3}", true)]
    [InlineData("NoMatch", "\\d+", false)]
    [InlineData("", "\\d+", false)]
    [InlineData(null, "\\d+", false)]
    public void RegexMatch_FromString_ShouldReturnExpectedMatch(
        string? source,
        string pattern,
        bool expectedSuccess)
    {
        var regex = new Regex(pattern);

        var result = source.RegexMatch(regex);

        Assert.NotNull(result);
        Assert.Equal(expectedSuccess, result.Success);
    }

    [Fact]
    public void RegexMatch_FromString_ShouldReturnEmptyMatchWhenRegexIsNull()
    {
        Regex? regex = null;

        var result = "Input123".RegexMatch(regex);

        Assert.Same(Match.Empty, result);
    }

    [Fact]
    public void RegexMatch_FromString_ShouldReturnEmptyMatchWhenSourceIsNull()
    {
        string? source = null;
        var regex = new Regex(@"\d+");

        var result = source.RegexMatch(regex);

        Assert.Same(Match.Empty, result);
    }

    [Fact]
    public void RegexMatch_ShouldReturnFirstMatch()
    {
        var regex = new Regex(@"\d+");

        var result = regex.RegexMatch("abc123def456");

        Assert.True(result.Success);
        Assert.Equal("123", result.Value);
        Assert.Equal(3, result.Index);
    }
}
