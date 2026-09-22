namespace Craft.Extensions.Tests.System;

public class OtherStringExtensionsTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("Test\r\nString", "Test\r\nString")]
    [InlineData("Multi\rLine\r\nText", "Multi\r\nLine\r\nText")]
    [InlineData("Carriage\rReturn", "Carriage\r\nReturn")]
    [InlineData("Line\nFeed", "Line\r\nFeed")]
    public void NormalizeLineEndings_ReturnsExpected(string? input, string? expected)
        => Assert.Equal(expected, input.NormalizeLineEndings());

    [Theory]
    [InlineData("abcabc", 'a', 1, 0)]
    [InlineData("abcabc", 'a', 2, 3)]
    [InlineData("abcabc", 'c', 2, 5)]
    [InlineData("abcabc", 'x', 1, -1)]
    [InlineData("", 'a', 1, -1)]
    [InlineData(null, 'a', 1, -1)]
    [InlineData("abc", 'a', 0, -1)]
    public void NthIndexOf_ReturnsExpected(string? input, char character, int occurrence, int expected)
        => Assert.Equal(expected, input.NthIndexOf(character, occurrence));

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("test", null, "test")]
    [InlineData(null, new[] { "abc" }, null)]
    [InlineData("source", new string[0], "source")]
    [InlineData("apple,orange,apple,banana", new[] { "apple", "banana" }, ",orange,,")]
    [InlineData("abcdef", new[] { "abc", "def" }, "")]
    [InlineData("hello", new string?[] { null, "", "he" }, "llo")]
    public void RemoveAll_ReturnsExpected(string? input, string[]? values, string? expected)
        => Assert.Equal(expected, input.RemoveAll(values));

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("  multiple   spaces   between   words  ", "multiple spaces between words")]
    [InlineData("  line\tbreak\nnext  ", "line break next")]
    public void RemoveExtraSpaces_ReturnsExpected(string? input, string? expected)
        => Assert.Equal(expected, input.RemoveExtraSpaces());

    [Theory]
    [InlineData("example.txt", "example", ".txt", ".TXT")]
    [InlineData("hello", "hello", "world")]
    [InlineData(null, null, "suffix")]
    public void RemovePostFix_Ordinal_ReturnsExpected(string? input, string? expected, params string[] postfixes)
        => Assert.Equal(expected, input.RemovePostFix(postfixes));

    [Fact]
    public void RemovePostFix_WithComparison_IsCaseInsensitive()
        => Assert.Equal("example", "example.TXT".RemovePostFix(StringComparison.OrdinalIgnoreCase, ".txt"));

    [Theory]
    [InlineData("test123", "123", "test")]
    [InlineData("hello", "hello", "world")]
    [InlineData(null, null, "prefix")]
    public void RemovePreFix_Ordinal_ReturnsExpected(string? input, string? expected, params string[] prefixes)
        => Assert.Equal(expected, input.RemovePreFix(prefixes));

    [Fact]
    public void RemovePreFix_WithComparison_IsCaseInsensitive()
        => Assert.Equal("example", "EXAMPLE".RemovePreFix(StringComparison.OrdinalIgnoreCase, "ex"));

    [Theory]
    [InlineData("hello world", "hello", "hi", StringComparison.Ordinal, "hi world")]
    [InlineData("test123test", "123", "456", StringComparison.OrdinalIgnoreCase, "test456test")]
    [InlineData("test123test", "789", "456", StringComparison.OrdinalIgnoreCase, "test123test")]
    [InlineData(null, "search", "replace", StringComparison.Ordinal, null)]
    [InlineData("", "search", "replace", StringComparison.Ordinal, "")]
    [InlineData("empty", "", "replacement", StringComparison.Ordinal, "empty")]
    public void ReplaceFirst_ReturnsExpected(
        string? input,
        string? search,
        string? replacement,
        StringComparison comparison,
        string? expected)
        => Assert.Equal(expected, input.ReplaceFirst(search, replacement, comparison));

    [Theory]
    [InlineData("", "D41D8CD98F00B204E9800998ECF8427E")]
    [InlineData("hello world", "5EB63BBBE01EEED093CB22BB8F5ACDC3")]
    public void ToSha256_ReturnsExpected(string input, string expected)
        => Assert.Equal(expected, input.ToSha256());

    [Fact]
    public void ToSha256_Null_Throws()
        => Assert.Throws<ArgumentNullException>(() => ((string?)null).ToSha256());

    [Theory]
    [InlineData("SGVsbG8=", true)]
    [InlineData("aGVsbG8gd29ybGQ=", true)]
    [InlineData("not-base64", false)]
    [InlineData("abc", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void IsBase64String_ReturnsExpected(string? input, bool expected)
        => Assert.Equal(expected, input.IsBase64String());

    [Theory]
    [InlineData("hello world", 5, "hello")]
    [InlineData("hello", 10, "hello")]
    [InlineData("hello", 5, "hello")]
    [InlineData(null, 5, null)]
    [InlineData("", 5, "")]
    [InlineData("hello", 0, "")]
    public void Truncate_ReturnsExpected(string? input, int maxLength, string? expected)
        => Assert.Equal(expected, input.Truncate(maxLength));

    [Fact]
    public void Truncate_NegativeLength_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => "hello".Truncate(-1));

    [Theory]
    [InlineData("hello world", 10, "...", "hello w...")]
    [InlineData("hello world", 8, "...", "hello...")]
    [InlineData("hello", 10, "...", "hello")]
    [InlineData("hello world", 2, "...", "he")]
    [InlineData(null, 10, "...", null)]
    public void TruncateWithEllipsis_ReturnsExpected(
        string? input, int maxLength, string ellipsis, string? expected)
        => Assert.Equal(expected, input.TruncateWithEllipsis(maxLength, ellipsis));

    [Fact]
    public void TruncateWithEllipsis_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => "hello".TruncateWithEllipsis(-1));
        Assert.Throws<ArgumentNullException>(() => "hello".TruncateWithEllipsis(3, null!));
    }

    [Theory]
    [InlineData("hello world", "HelloWorld")]
    [InlineData("hello_world", "HelloWorld")]
    [InlineData("hello-world", "HelloWorld")]
    [InlineData("HELLO WORLD", "HelloWorld")]
    [InlineData(null, null)]
    [InlineData("", "")]
    public void ToPascalCase_ReturnsExpected(string? input, string? expected)
        => Assert.Equal(expected, input.ToPascalCase());

    [Theory]
    [InlineData("hello world", "helloWorld")]
    [InlineData("hello_world", "helloWorld")]
    [InlineData("hello-world", "helloWorld")]
    [InlineData("HELLO WORLD", "helloWorld")]
    [InlineData("Hello", "hello")]
    [InlineData(null, null)]
    public void ToCamelCase_ReturnsExpected(string? input, string? expected)
        => Assert.Equal(expected, input.ToCamelCase());

    [Theory]
    [InlineData("HelloWorld123", "hello_world123")]
    [InlineData("helloWorld", "hello_world")]
    [InlineData("hello world", "hello_world")]
    [InlineData("hello-world", "hello_world")]
    [InlineData("HTTPSConnection", "httpsconnection")]
    [InlineData(null, null)]
    public void ToSnakeCase_ReturnsExpected(string? input, string? expected)
        => Assert.Equal(expected, input.ToSnakeCase());

    [Theory]
    [InlineData("HelloWorld123", "hello-world123")]
    [InlineData("helloWorld", "hello-world")]
    [InlineData("hello world", "hello-world")]
    [InlineData("hello_world", "hello-world")]
    [InlineData("HTTPSConnection", "httpsconnection")]
    [InlineData(null, null)]
    public void ToKebabCase_ReturnsExpected(string? input, string? expected)
        => Assert.Equal(expected, input.ToKebabCase());

    [Theory]
    [InlineData("hello", "olleh")]
    [InlineData("a", "a")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void Reverse_ReturnsExpected(string? input, string? expected)
        => Assert.Equal(expected, input.Reverse());

    [Fact]
    public void Reverse_PreservesUnicodeTextElements()
        => Assert.Equal("😀e\u0301", "e\u0301😀".Reverse());

    [Theory]
    [InlineData("hello world hello", "hello", StringComparison.Ordinal, 2)]
    [InlineData("HELLO hello HeLLo", "hello", StringComparison.OrdinalIgnoreCase, 3)]
    [InlineData("aaaa", "aa", StringComparison.Ordinal, 2)]
    [InlineData(null, "hello", StringComparison.Ordinal, 0)]
    [InlineData("hello", null, StringComparison.Ordinal, 0)]
    public void CountOccurrences_ReturnsExpected(
        string? input, string? substring, StringComparison comparison, int expected)
        => Assert.Equal(expected, input.CountOccurrences(substring, comparison));

    [Theory]
    [InlineData("12345", true)]
    [InlineData("123abc", false)]
    [InlineData("٠١٢", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsNumeric_ReturnsExpected(string? input, bool expected)
        => Assert.Equal(expected, input.IsNumeric());

    [Theory]
    [InlineData("hello", true)]
    [InlineData("hello123", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsAlphabetic_ReturnsExpected(string? input, bool expected)
        => Assert.Equal(expected, input.IsAlphabetic());

    [Theory]
    [InlineData("hello123", true)]
    [InlineData("12345", true)]
    [InlineData("hello_123", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsAlphanumeric_ReturnsExpected(string? input, bool expected)
        => Assert.Equal(expected, input.IsAlphanumeric());
}
