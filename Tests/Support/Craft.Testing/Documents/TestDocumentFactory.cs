using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Craft.Testing.Documents;

/// <summary>Creates small document inputs for tests without checked-in binary files.</summary>
public static class TestDocumentFactory
{
    /// <summary>Creates a Word document with a single paragraph containing the supplied text.</summary>
    public static void CreateDocx(string filePath, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(text);

        using var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
        var part = document.AddMainDocumentPart();
        part.Document = new Document(new Body(new Paragraph(new Run(new Text(text)))));
    }

    /// <summary>Creates a one-page PDF using standard Helvetica. Text should use supported Latin characters.</summary>
    public static void CreatePdf(string filePath, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(text);

        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(PageSize.A4);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        page.AddText(text, 12, new PdfPoint(25, 700), font);
        File.WriteAllBytes(filePath, builder.Build());
    }
}
