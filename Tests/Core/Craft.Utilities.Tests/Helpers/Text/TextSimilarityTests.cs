using Craft.Utilities.Helpers.Text;

namespace Craft.Utilities.Tests.Helpers.Text;

public class TextSimilarityTests
{
    #region Public Methods

    [Theory]
    [InlineData(null, null, 0)]
    [InlineData("", null, 0)]
    [InlineData(null, "", 0)]
    [InlineData("abc", "xyz", 3)]
    [InlineData("abc", "a", 2)]
    [InlineData("é", "e", 1)]
    [InlineData("😀", "", 2)]
    public void LevenshteinDistance_EdgeCases_ReturnsUtf16Distance(string? source, string? target, int expected)
        => Assert.Equal(expected, TextSimilarity.LevenshteinDistance(source, target));

    [Fact]
    public void LevenshteinDistance_EmptySource_ReturnsTargetLength()
    {
        // Arrange / Act
        var result = TextSimilarity.LevenshteinDistance(string.Empty, "Bhairav");

        // Assert
        Assert.Equal(7, result);
    }

    [Fact]
    public void LevenshteinDistance_EmptyTarget_ReturnsSourceLength()
    {
        // Arrange / Act
        var result = TextSimilarity.LevenshteinDistance("Bhairav", string.Empty);

        // Assert
        Assert.Equal(7, result);
    }

    [Fact]
    public void LevenshteinDistance_HighlyUnequalLengths_HandlesBothDirections()
    {
        string longText = new('x', 100000);
        Assert.Equal(99999, TextSimilarity.LevenshteinDistance("x", longText));
        Assert.Equal(99999, TextSimilarity.LevenshteinDistance(longText, "x"));
    }

    // ── LevenshteinDistance ──────────────────────────────────────────────────

    [Fact]
    public void LevenshteinDistance_IdenticalStrings_ReturnsZero()
    {
        // Arrange / Act
        var result = TextSimilarity.LevenshteinDistance("Yaman", "Yaman");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void LevenshteinDistance_IsCaseSensitive()
    {
        // Act — "yaman" vs "Yaman" differs by one character (case)
        var result = TextSimilarity.LevenshteinDistance("yaman", "Yaman");

        // Assert
        Assert.Equal(1, result);
    }

    [Theory]
    [InlineData("Yaman", "Yamani")]
    [InlineData("Todi", "Todi ")]
    [InlineData("Bhairav", "Bhairavi")]
    public void LevenshteinDistance_IsSymmetric(string a, string b)
    {
        // Act
        var ab = TextSimilarity.LevenshteinDistance(a, b);
        var ba = TextSimilarity.LevenshteinDistance(b, a);

        // Assert
        Assert.Equal(ab, ba);
    }

    [Theory]
    [InlineData("kitten", "sitting", 3)]   // classic example
    [InlineData("Ahir", "Aheer", 2)]       // musical-domain variant spelling
    [InlineData("Bhairav", "Bhairavi", 1)] // single insertion
    [InlineData("abc", "abc", 0)]          // identical
    [InlineData("a", "b", 1)]              // single substitution
    public void LevenshteinDistance_KnownPairs_ReturnsExpectedDistance(
        string source, string target, int expected)
    {
        // Act
        var result = TextSimilarity.LevenshteinDistance(source, target);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void LevenshteinDistance_NullSource_ReturnsTargetLength()
    {
        // Arrange / Act
        var result = TextSimilarity.LevenshteinDistance(null!, "Todi");

        // Assert
        Assert.Equal(4, result);
    }

    [Fact]
    public void LevenshteinDistance_NullTarget_ReturnsSourceLength()
    {
        // Arrange / Act
        var result = TextSimilarity.LevenshteinDistance("Todi", null!);

        // Assert
        Assert.Equal(4, result);
    }

    #endregion Public Methods
}
