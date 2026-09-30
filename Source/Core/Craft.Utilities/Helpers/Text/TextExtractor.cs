using System.Text;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig.Core;

namespace Craft.Utilities.Helpers;

public static class TextExtractor
{
    /// <summary>Extracts text from a PDF or DOCX file. Binary DOC is not supported.</summary>
    /// <remarks>Malformed supported documents return empty for compatibility; file access errors propagate.
    /// This extracts existing text and does not perform OCR on scanned pages.</remarks>
    public static string ExtractTextFromDocOrPdf(string fileName)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (extension is not ".pdf" and not ".docx")
            throw new NotSupportedException("Only PDF and DOCX files are supported; legacy binary DOC files require a different reader.");

        if (!File.Exists(fileName))
            throw new FileNotFoundException("The file does not exist", fileName);

        if (extension == ".pdf")
        {
            // Use file path directly for PDF
            return ExtractTextFromPdf(fileName);
        }
        else
        {
            using (var stream = File.OpenRead(fileName))
            {
                return ExtractTextFromWordDocument(stream);
            }
        }
    }

    private static string ExtractTextFromPdf(string fileName)
    {
        try
        {
            var sb = new StringBuilder();

            using var document = UglyToad.PdfPig.PdfDocument.Open(fileName);

            foreach (var page in document.GetPages())
                sb.Append(page.Text);

            return sb.ToString();
        }
        catch (PdfDocumentFormatException)
        {
            return string.Empty;
        }
    }

    private static string ExtractTextFromWordDocument(Stream stream)
    {
        try
        {
            using var doc = WordprocessingDocument.Open(stream, false);
            return doc.MainDocumentPart?.Document?.Body?.InnerText ?? string.Empty;
        }
        catch (Exception error) when (error is OpenXmlPackageException or InvalidDataException)
        {
            return string.Empty;
        }
    }
}
