namespace Craft.Extensions.Tests.System;

public class StandardStringExtensionsTests
{
    [Theory]
    [InlineData("abc", 'c', StringComparison.Ordinal, "abc")]
    [InlineData("Hello", 'o', StringComparison.OrdinalIgnoreCase, "Hello")]
    [InlineData("world", '!', StringComparison.Ordinal, "world!")]
    [InlineData("", 'a', StringComparison.Ordinal, "a")]
    [InlineData(null, 'x', StringComparison.Ordinal, null)]
    public void EnsureEndsWith_ReturnsExpected(
        string? source, char character, StringComparison comparison, string? expected)
        => Assert.Equal(expected, source.EnsureEndsWith(character, comparison));

    [Theory]
    [InlineData("abc", 'a', StringComparison.Ordinal, "abc")]
    [InlineData("world", 'h', StringComparison.OrdinalIgnoreCase, "hworld")]
    [InlineData("123", '0', StringComparison.Ordinal, "0123")]
    [InlineData("", 'a', StringComparison.Ordinal, "a")]
    [InlineData(null, 'x', StringComparison.Ordinal, null)]
    public void EnsureStartsWith_ReturnsExpected(
        string? source, char character, StringComparison comparison, string? expected)
        => Assert.Equal(expected, source.EnsureStartsWith(character, comparison));

    [Theory]
    [InlineData("hello", "Hello")]
    [InlineData("WORLD", "WORLD")]
    [InlineData("123", "123")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void FirstCharToUpper_ReturnsExpected(string? input, string? expected)
        => Assert.Equal(expected, input.FirstCharToUpper());

    [Theory]
    [InlineData("example.file.txt", '.', "txt")]
    [InlineData("path/to/some/file.txt", '/', "file.txt")]
    [InlineData(null, '.', null)]
    [InlineData("no_delimiter", '.', "no_delimiter")]
    [InlineData("delimiter_at_the_end.", '.', "")]
    [InlineData(".hidden", '.', "hidden")]
    public void GetStringAfterLastDelimiter_ReturnsExpected(
        string? input, char delimiter, string? expected)
        => Assert.Equal(expected, input.GetStringAfterLastDelimiter(delimiter));

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("abc", false)]
    public void IsNullOrEmpty_ReturnsExpected(string? input, bool expected)
        => Assert.Equal(expected, input.IsNullOrEmpty());

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("abc", false)]
    [InlineData("  abc  ", false)]
    public void IsNullOrWhiteSpace_ReturnsExpected(string? input, bool expected)
        => Assert.Equal(expected, input.IsNullOrWhiteSpace());

    [Theory]
    [InlineData(null, 5, null)]
    [InlineData("", 5, "")]
    [InlineData("abcdef", 0, "")]
    [InlineData("abcdef", 5, "abcde")]
    [InlineData("abcdef", 10, "abcdef")]
    public void Left_ReturnsExpected(string? source, int length, string? expected)
        => Assert.Equal(expected, source.Left(length));

    [Theory]
    [InlineData(null, 5, null)]
    [InlineData("", 5, "")]
    [InlineData("abcdef", 0, "")]
    [InlineData("abcdef", 5, "bcdef")]
    [InlineData("abcdef", 10, "abcdef")]
    public void Right_ReturnsExpected(string? source, int length, string? expected)
        => Assert.Equal(expected, source.Right(length));

    [Fact]
    public void LeftAndRight_NegativeLength_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => "abcdef".Left(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => "abcdef".Right(-1));
    }
}
