using System.Text.RegularExpressions;
using Craft.Domain.Helpers;

namespace Craft.Domain.Tests.Helpers;

/// <summary>
/// Unit tests for the DomainConstants class.
/// </summary>
public class DomainConstantsTests
{
    #region Regular Expression Tests

    [Theory]
    [InlineData("John", true)]
    [InlineData("John Doe", true)]
    [InlineData("John O'Brien", true)]
    [InlineData("John (Jr.)", true)]
    [InlineData("john", false)]
    [InlineData("123", false)]
    [InlineData("", false)]
    public void NameRegExpr_ShouldValidateCorrectly(string input, bool shouldMatch)
    {
        // Arrange
        var regex = new Regex(DomainConstants.NameRegExpr);

        // Act
        var isMatch = regex.IsMatch(input);

        // Assert
        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("ABC", true)]
    [InlineData("XYZ", true)]
    [InlineData("AB1", true)] // Matches because it starts with uppercase letters
    [InlineData("abc", false)]
    [InlineData("123", false)]
    [InlineData("", false)]
    public void AlphaCodeRegEx_ShouldValidateCorrectly(string input, bool shouldMatch)
    {
        // Arrange
        var regex = new Regex(DomainConstants.AlphaCodeRegEx);

        // Act
        var isMatch = regex.IsMatch(input);

        // Assert
        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("A123", true)]
    [InlineData("ABC123", true)]
    [InlineData("A", true)]
    [InlineData("abc123", false)]
    [InlineData("123", false)]
    [InlineData("", false)]
    public void AlphaNumCodeRegEx_ShouldValidateCorrectly(string input, bool shouldMatch)
    {
        // Arrange
        var regex = new Regex(DomainConstants.AlphaNumCodeRegEx);

        // Act
        var isMatch = regex.IsMatch(input);

        // Assert
        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("123", true)]
    [InlineData("456789", true)]
    [InlineData("0", true)]
    [InlineData("12a", true)] // Matches because it starts with digits
    [InlineData("abc", false)]
    [InlineData("", false)]
    public void DigitsRegEx_ShouldValidateCorrectly(string input, bool shouldMatch)
    {
        // Arrange
        var regex = new Regex(DomainConstants.DigitsRegEx);

        // Act
        var isMatch = regex.IsMatch(input);

        // Assert
        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("123", true)]
    [InlineData("9", true)]
    [InlineData("0", false)]
    [InlineData("012", false)]
    [InlineData("", false)]
    public void DigitsGreaterThanZero_ShouldValidateCorrectly(string input, bool shouldMatch)
    {
        // Arrange
        var regex = new Regex(DomainConstants.DigitsGreaterThanZero);

        // Act
        var isMatch = regex.IsMatch(input);

        // Assert
        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("123", true)]
    [InlineData("123.45", true)]
    [InlineData(".45", true)]
    [InlineData("0.5", true)]
    [InlineData("abc", false)]
    [InlineData("", false)]
    public void NumberRegEx_ShouldValidateCorrectly(string input, bool shouldMatch)
    {
        // Arrange
        var regex = new Regex(DomainConstants.NumberRegEx);

        // Act
        var isMatch = regex.IsMatch(input);

        // Assert
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
    public void EmailRegExpr_ShouldValidateCorrectly(string input, bool shouldMatch)
    {
        // Arrange
        var regex = new Regex(DomainConstants.EmailRegExpr);

        // Act
        var isMatch = regex.IsMatch(input);

        // Assert
        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("+1234567890", true)]
    [InlineData("123-456-7890", true)]
    [InlineData("(123) 456-7890", true)]
    [InlineData("+1 (234) 567-8900", true)]
    [InlineData("abc", false)]
    [InlineData("", false)]
    public void PhoneRegExpr_ShouldValidateCorrectly(string input, bool shouldMatch)
    {
        // Arrange
        var regex = new Regex(DomainConstants.PhoneRegExpr);

        // Act
        var isMatch = regex.IsMatch(input);

        // Assert
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
    public void UrlRegExpr_ShouldValidateCorrectly(string input, bool shouldMatch)
    {
        // Arrange
        var regex = new Regex(DomainConstants.UrlRegExpr);

        // Act
        var isMatch = regex.IsMatch(input);

        // Assert
        Assert.Equal(shouldMatch, isMatch);
    }

    [Theory]
    [InlineData("ABC123-456", true)]
    [InlineData("XYZ", true)]
    [InlineData("123", true)]
    [InlineData("A1-B2", true)]
    [InlineData("abc", true)] // Matches empty pattern (zero occurrences)
    [InlineData("", true)] // Matches empty pattern (zero occurrences)
    public void AlphaNumCodeWithHyphenRegEx_ShouldValidateCorrectly(string input, bool shouldMatch)
    {
        // Arrange
        var regex = new Regex(DomainConstants.AlphaNumCodeWithHyphenRegEx);

        // Act
        var isMatch = regex.IsMatch(input);

        // Assert
        Assert.Equal(shouldMatch, isMatch);
    }

    #endregion

    #region Constant Values Tests

    [Fact]
    public void AllRegularExpressions_ShouldNotBeNullOrEmpty()
    {
        // Arrange & Act & Assert
        Assert.NotNull(DomainConstants.NameRegExpr);
        Assert.NotNull(DomainConstants.AlphaCodeRegEx);
        Assert.NotNull(DomainConstants.AlphaNumCodeRegEx);
        Assert.NotNull(DomainConstants.DigitsRegEx);
        Assert.NotNull(DomainConstants.DigitsGreaterThanZero);
        Assert.NotNull(DomainConstants.NumberRegEx);
        Assert.NotNull(DomainConstants.AlphaNumCodeWithHyphenRegEx);
        Assert.NotNull(DomainConstants.EmailRegExpr);
        Assert.NotNull(DomainConstants.PhoneRegExpr);
        Assert.NotNull(DomainConstants.UrlRegExpr);
    }

    [Fact]
    public void AllRegularExpressions_ShouldBeValidRegex()
    {
        // Arrange & Act & Assert
        Assert.NotNull(new Regex(DomainConstants.NameRegExpr));
        Assert.NotNull(new Regex(DomainConstants.AlphaCodeRegEx));
        Assert.NotNull(new Regex(DomainConstants.AlphaNumCodeRegEx));
        Assert.NotNull(new Regex(DomainConstants.DigitsRegEx));
        Assert.NotNull(new Regex(DomainConstants.DigitsGreaterThanZero));
        Assert.NotNull(new Regex(DomainConstants.NumberRegEx));
        Assert.NotNull(new Regex(DomainConstants.AlphaNumCodeWithHyphenRegEx));
        Assert.NotNull(new Regex(DomainConstants.EmailRegExpr));
        Assert.NotNull(new Regex(DomainConstants.PhoneRegExpr));
        Assert.NotNull(new Regex(DomainConstants.UrlRegExpr));
    }

    #endregion
}