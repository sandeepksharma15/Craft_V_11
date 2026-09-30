using System.Text;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Craft.Utilities.Helpers.Text;

/// <summary>
/// Converts basic CommonMark formatting to RTF without interpreting user text as RTF controls.
/// </summary>
public static class TextConverters
{
    #region Private Methods

    private static void AppendText(StringBuilder rtf, string text)
    {
        foreach (char character in text)
        {
            switch (character)
            {
                case '\\':
                case '{':
                case '}':
                    rtf.Append('\\').Append(character);
                    break;

                case '\r':
                    break;

                case '\n':
                    rtf.Append(@"\line ");
                    break;

                case '\t':
                    rtf.Append(@"\tab ");
                    break;

                default:
                    if (character > 127)
                        rtf.Append(@"\u").Append(((short)character).ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('?');
                    else
                        rtf.Append(character);
                    break;
            }
        }
    }

    private static void RenderBlocks(StringBuilder rtf, ContainerBlock blocks)
    {
        foreach (Block block in blocks)
        {
            switch (block)
            {
                case HeadingBlock heading:
                    int size = heading.Level switch { 1 => 36, 2 => 32, 3 => 28, 4 => 24, 5 => 22, _ => 20 };
                    rtf.Append(@"\b\fs").Append(size).Append(' ');
                    RenderInlines(rtf, heading.Inline!);
                    rtf.Append(@"\b0\fs24\par ");
                    break;

                case ParagraphBlock paragraph:
                    RenderInlines(rtf, paragraph.Inline!);
                    rtf.Append(@"\par ");
                    break;

                case CodeBlock code:
                    AppendText(rtf, code.Lines.ToString());
                    rtf.Append(@"\par ");
                    break;

                case HtmlBlock html:
                    AppendText(rtf, html.Lines.ToString());
                    rtf.Append(@"\par ");
                    break;

                case ListBlock list:
                    int number = int.Parse(list.OrderedStart ?? "1", System.Globalization.CultureInfo.InvariantCulture);
                    foreach (Block item in list)
                    {
                        if (list.IsOrdered)
                            rtf.Append(number++).Append(@".\tab ");
                        else
                            rtf.Append(@"\bullet\tab ");
                        RenderBlocks(rtf, (ContainerBlock)item);
                    }
                    break;

                case ThematicBreakBlock:
                    rtf.Append(@"\par ");
                    break;

                case ContainerBlock container:
                    rtf.Append(@"{\li360 ");
                    RenderBlocks(rtf, container);
                    rtf.Append('}');
                    break;
            }
        }
    }

    private static void RenderInlines(StringBuilder rtf, ContainerInline container)
    {
        for (Inline? inline = container.FirstChild; inline is not null; inline = inline.NextSibling)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    AppendText(rtf, literal.Content.ToString());
                    break;

                case HtmlEntityInline entity:
                    AppendText(rtf, entity.Transcoded.ToString());
                    break;

                case HtmlInline html:
                    AppendText(rtf, html.Tag);
                    break;

                case LineBreakInline lineBreak:
                    rtf.Append(lineBreak.IsHard ? @"\line " : " ");
                    break;

                case CodeInline code:
                    AppendText(rtf, code.Content);
                    break;

                case EmphasisInline emphasis:
                    bool bold = emphasis.DelimiterCount == 2;
                    rtf.Append(bold ? @"{\b " : @"{\i ");
                    RenderInlines(rtf, emphasis);
                    rtf.Append(bold ? @"\b0 }" : @"\i0 }");
                    break;

                case LinkInline link:
                    if (link.IsImage)
                        RenderInlines(rtf, link);
                    else
                    {
                        StartLink(rtf, link.Url!);
                        RenderInlines(rtf, link);
                        rtf.Append("}}");
                    }
                    break;

                case AutolinkInline link:
                    StartLink(rtf, link.IsEmail ? "mailto:" + link.Url : link.Url);
                    AppendText(rtf, link.Url);
                    rtf.Append("}}");
                    break;
            }
        }
    }

    private static void StartLink(StringBuilder rtf, string url)
    {
        rtf.Append(@"{\field{\*\fldinst{HYPERLINK """);
        // Quotes must not terminate the field instruction's URL.
        AppendText(rtf, url.Replace("\"", "%22"));
        rtf.Append(@"""}}{\fldrslt ");
    }

    #endregion Private Methods

    #region Public Methods

    /// <summary>
    /// Converts headings, paragraphs, emphasis, lists, links, quotes and code to Unicode RTF.
    /// </summary>
    /// <remarks>
    /// Images render their alternative text. Raw HTML is displayed as literal text. This is a basic
    /// text converter, not a general HTML renderer or page-layout engine.
    /// </remarks>
    public static string ConvertMarkdownToRtf(string? markdown)
    {
        ArgumentException.ThrowIfNullOrEmpty(markdown);
        StringBuilder rtf = new(@"{\rtf1\ansi\deff0\uc1 ");
        RenderBlocks(rtf, Markdown.Parse(markdown));
        return rtf.Append('}').ToString();
    }

    #endregion Public Methods
}
