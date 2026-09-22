using System.Text.RegularExpressions;

namespace Craft.Extensions.Tests.System;

public class RegexExtensionsTests
{
    [Theory]
    [InlineData("Test123", @"\d+", true)]
    [InlineData("NoDigits", @"\d+", false)]
    [InlineData(null, @"\d+", false)]
    public void RegexMatch_II_ShouldReturnExpectedResult(string? source, string? pattern, bool expectedSuccess)
    {
        var regex = new Regex(pattern!);

        var result = source!.RegexMatch(regex);

        Assert.NotNull(result);
        Assert.Equal(expectedSuccess, result.Success);
    }

    [Theory]
    [InlineData("Input123", "Input\d+", true)]
    [InlineData("Test123", "\d{3}", true)]
    [InlineData("NoMatch", "\d+", false)]
    [InlineData(null, "\d+", false)]
    [InlineData("Input123", null, false)]
    public void RegexMatch_ShouldReturnExpectedResult(string? input, string? pattern, bool expectedResult)
    {
        Regex? regex = pattern != null
            ? new Regex(pattern)
            : null;

        Match result = regex!.RegexMatch(input!);

        Assert.NotNull(result);
        Assert.Equal(expectedResult, result.Success);
    }
}
