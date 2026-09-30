using Craft.Testing.Documents;
using Craft.Testing.IO;
using Craft.Utilities.Helpers.Text;

namespace Craft.Utilities.Tests.Helpers.Text;

public class TextExtractorTests
{
    #region Public Methods

    [Fact]
    public void ExtractTextFromDocOrPdf_EmptyWordDocument_ReturnsEmptyAndReleasesFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.DOCX");
        try
        {
            using (var document = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(path,
                DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
            {
                var part = document.AddMainDocumentPart();
                part.Document = new(new DocumentFormat.OpenXml.Wordprocessing.Body());
            }
            Assert.Equal("", TextExtractor.ExtractTextFromDocOrPdf(path));
            using FileStream exclusive = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("legacy.doc")]
    [InlineData("legacy.DOC")]
    public void ExtractTextFromDocOrPdf_LegacyBinaryWord_IsExplicitlyUnsupported(string path)
        => Assert.Throws<NotSupportedException>(() => TextExtractor.ExtractTextFromDocOrPdf(path));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExtractTextFromDocOrPdf_MissingWordPartOrDocument_ReturnsEmpty(bool addPart)
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.docx");
        try
        {
            using (var document = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(path,
                DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
            {
                if (addPart) document.AddMainDocumentPart();
            }
            Assert.Equal("", TextExtractor.ExtractTextFromDocOrPdf(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ExtractTextFromDocOrPdf_ReturnsEmptyString_ForCorruptedDocx()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.docx");

        File.WriteAllText(tempFile, "not a real docx");
        try
        {
            var result = TextExtractor.ExtractTextFromDocOrPdf(tempFile);
            Assert.Equal(string.Empty, result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ExtractTextFromDocOrPdf_ReturnsEmptyString_ForCorruptedPdf()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
        File.WriteAllText(tempFile, "not a real pdf");
        try
        {
            var result = TextExtractor.ExtractTextFromDocOrPdf(tempFile);
            Assert.Equal(string.Empty, result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData("Hello World")]
    [InlineData("Generated document: 123!")]
    public void ExtractTextFromDocOrPdf_ReturnsText_ForValidDocx(string text)
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        string filePath = Path.Combine(directory.DirectoryPath, "document.docx");
        TestDocumentFactory.CreateDocx(filePath, text);

        // Act
        var result = TextExtractor.ExtractTextFromDocOrPdf(filePath);

        // Assert
        Assert.Equal(text, result);
        using FileStream exclusive = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    [Theory]
    [InlineData("Hello World")]
    [InlineData("Generated document: 123!")]
    public void ExtractTextFromDocOrPdf_ReturnsText_ForValidPdf(string text)
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        string filePath = Path.Combine(directory.DirectoryPath, "document.pdf");
        TestDocumentFactory.CreatePdf(filePath, text);

        // Act
        var result = TextExtractor.ExtractTextFromDocOrPdf(filePath);

        // Assert
        Assert.Equal(text, result.Trim());
        using FileStream exclusive = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ExtractTextFromDocOrPdf_ThrowsArgumentException_WhenFileNameIsNullOrEmpty(string? fileName)
    {
        if (fileName is null)
            Assert.Throws<ArgumentNullException>(() => TextExtractor.ExtractTextFromDocOrPdf(fileName!));
        else
            Assert.Throws<ArgumentException>(() => TextExtractor.ExtractTextFromDocOrPdf(fileName));
    }

    [Fact]
    public void ExtractTextFromDocOrPdf_ThrowsFileNotFoundException_WhenFileDoesNotExist()
    {
        Assert.Throws<FileNotFoundException>(() => TextExtractor.ExtractTextFromDocOrPdf("nonexistent.docx"));
    }

    [Theory]
    [InlineData("test.txt")]
    [InlineData("test.xlsx")]
    [InlineData("test.png")]
    public void ExtractTextFromDocOrPdf_ThrowsNotSupportedException_ForUnsupportedExtensions(string fileName)
    {
        Assert.Throws<NotSupportedException>(() => TextExtractor.ExtractTextFromDocOrPdf(fileName));
    }

    [Fact]
    public void ExtractTextFromDocOrPdf_WordWithoutBody_ReturnsEmpty()
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.docx");
        try
        {
            using (var document = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(path,
                DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
            {
                document.AddMainDocumentPart().Document = new();
            }
            Assert.Equal("", TextExtractor.ExtractTextFromDocOrPdf(path));
        }
        finally { File.Delete(path); }
    }

    #endregion Public Methods
}
