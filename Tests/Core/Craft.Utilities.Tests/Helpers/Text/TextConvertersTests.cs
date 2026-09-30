using Craft.Utilities.Helpers.Text;

namespace Craft.Utilities.Tests.Helpers.Text;

public class TextConvertersTests
{
    #region Private Methods

    private static void AssertWellFormedRtf(string rtf)
    {
        Assert.StartsWith(@"{\rtf1", rtf);
        int depth = 0;
        for (int i = 0; i < rtf.Length; i++)
        {
            if (rtf[i] == '\\')
            {
                i++;
                continue;
            }
            if (rtf[i] == '{') depth++;
            if (rtf[i] == '}') depth--;
            Assert.True(depth >= 0);
        }
        Assert.Equal(0, depth);
    }

    #endregion Private Methods

    #region Public Methods

    [Fact]
    public void ConvertMarkdownToRtf_BoldText_ConvertsToRtfBold()
    {
        var rtf = TextConverters.ConvertMarkdownToRtf("**bold**");
        Assert.Contains(@"\b ", rtf);
        Assert.Contains(@"\b0 ", rtf);
    }

    [Theory]
    [InlineData("## Second", @"\fs32 ")]
    [InlineData("### Third", @"\fs28 ")]
    [InlineData("#### Fourth", @"\fs24 ")]
    [InlineData("##### Fifth", @"\fs22 ")]
    [InlineData("###### Sixth", @"\fs20 ")]
    [InlineData("first  \nsecond", @"\line ")]
    [InlineData("first\nsecond", "first second")]
    [InlineData("3. third\n4. fourth", @"3.\tab ")]
    [InlineData("> quoted", @"{\li360 ")]
    [InlineData("---", @"\par ")]
    [InlineData("`{code}`", @"\{code\}")]
    [InlineData("```\n{code}\nnext\n```", @"\{code\}\line next")]
    [InlineData("<div>raw</div>", "<div>raw</div>")]
    [InlineData("text <span>raw</span>", "<span>raw</span>")]
    [InlineData("![alternative](image.png)", "alternative")]
    [InlineData("[site](https://example.com)", "HYPERLINK \"https://example.com\"")]
    [InlineData("<https://example.com>", "HYPERLINK \"https://example.com\"")]
    [InlineData("<test@example.com>", "HYPERLINK \"mailto:test@example.com\"")]
    public void ConvertMarkdownToRtf_CommonMarkConstructs_RenderSupportedContent(string markdown, string expected)
    {
        string rtf = TextConverters.ConvertMarkdownToRtf(markdown);
        Assert.Contains(expected, rtf);
        AssertWellFormedRtf(rtf);
    }

    [Fact]
    public void ConvertMarkdownToRtf_HandlesSpecialCharacters()
    {
        var rtf = TextConverters.ConvertMarkdownToRtf("&amp; &quot; – ---; &nbsp;");
        Assert.Contains("&", rtf);
        Assert.Contains("\"", rtf);
        Assert.Contains(@"\u8211?", rtf); // Unicode en dash
        Assert.Contains(" ", rtf); // nbsp replaced by space
    }

    [Fact]
    public void ConvertMarkdownToRtf_Heading1_ConvertsToRtfHeading()
    {
        var rtf = TextConverters.ConvertMarkdownToRtf("# Heading 1");
        Assert.Contains(@"\b\fs36 ", rtf);
        Assert.Contains(@"\b0\fs24\par ", rtf);
    }

    [Fact]
    public void ConvertMarkdownToRtf_Hyperlink_ConvertsToRtfHyperlink()
    {
        var rtf = TextConverters.ConvertMarkdownToRtf("[mail](mailto:test@example.com)");
        Assert.Contains("HYPERLINK \"mailto:test@example.com\"", rtf);
        Assert.Contains("fldrslt mail", rtf);
    }

    [Fact]
    public void ConvertMarkdownToRtf_ItalicText_ConvertsToRtfItalic()
    {
        var rtf = TextConverters.ConvertMarkdownToRtf("*italic*");
        Assert.Contains(@"\i ", rtf);
        Assert.Contains(@"\i0 ", rtf);
    }

    [Fact]
    public void ConvertMarkdownToRtf_List_ConvertsToRtfBullet()
    {
        var rtf = TextConverters.ConvertMarkdownToRtf("- item1\n- item2");
        Assert.Contains(@"\bullet\tab ", rtf);
    }

    [Theory]
    [InlineData(@"{\rtf1 injected}", @"\{\\rtf1 injected\}")]
    [InlineData("&lt; &gt; &amp; &quot; &nbsp;", "< > & \" ")]
    [InlineData("café हिन्दी 😀", @"caf\u233?")]
    [InlineData("***nested***", @"{\i {\b ")]
    [InlineData("    a\tb\r\n    c", @"a\tab b\line c")]
    [InlineData("---;", "---;")]
    public void ConvertMarkdownToRtf_LiteralText_EscapesControlsAndPreservesContent(string markdown, string expected)
    {
        string rtf = TextConverters.ConvertMarkdownToRtf(markdown);
        Assert.Contains(expected, rtf);
        AssertWellFormedRtf(rtf);
    }

    [Fact]
    public void ConvertMarkdownToRtf_MixedContent_AllowsMultipleFeatures()
    {
        var rtf = TextConverters.ConvertMarkdownToRtf("# Title\n\n**bold** and *italic* and [mail](mailto:test@example.com)\n- item");
        Assert.Contains(@"\b\fs36 ", rtf);
        Assert.Contains(@"\b ", rtf);
        Assert.Contains(@"\i ", rtf);
        Assert.Contains("HYPERLINK \"mailto:test@example.com\"", rtf);
        Assert.Contains(@"\bullet\tab ", rtf);
    }

    [Fact]
    public void ConvertMarkdownToRtf_NullOrEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() => TextConverters.ConvertMarkdownToRtf(null));
        Assert.Throws<ArgumentException>(() => TextConverters.ConvertMarkdownToRtf(""));
    }

    [Fact]
    public void ConvertMarkdownToRtf_Paragraph_ConvertsToRtfParagraph()
    {
        var rtf = TextConverters.ConvertMarkdownToRtf("Paragraph\n\nAnother");
        Assert.Contains(@"\par ", rtf);
    }

    [Fact]
    public void ConvertMarkdownToRtf_QuotesInLink_DoNotTerminateUrl()
    {
        string rtf = TextConverters.ConvertMarkdownToRtf("[label](<https://example.com/a%20b?x=&quot;>)");
        Assert.Contains("%22", rtf);
        AssertWellFormedRtf(rtf);
    }

    [Fact]
    public void ConvertMarkdownToRtf_Unicode_UsesSignedUtf16Escapes()
    {
        string rtf = TextConverters.ConvertMarkdownToRtf("😀");
        Assert.Contains(@"\u-10179?\u-8704?", rtf);
        Assert.DoesNotContain("😀", rtf);
    }

    #endregion Public Methods
}
