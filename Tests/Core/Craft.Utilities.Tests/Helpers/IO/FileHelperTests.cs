using System.Security.Cryptography;
using Craft.Utilities.Helpers;

namespace Craft.Utilities.Tests.Helpers;

public class FileHelperTests : IDisposable
{

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("folder/test.txt")]
    public void GetUniqueFileName_PathInsteadOfName_Throws(string name)
        => Assert.Throws<ArgumentException>("fileName", () => FileHelper.GetUniqueFileName(_testDirectory, name));

    [Fact]
    public void GetUniqueFileName_DirectoryCollision_UsesCounter()
    {
        Directory.CreateDirectory(Path.Combine(_testDirectory, "report"));
        Assert.Equal("report_1", FileHelper.GetUniqueFileName(_testDirectory, "report"));
    }

    [Theory]
    [InlineData("SHA256", 64)]
    [InlineData("SHA384", 96)]
    [InlineData("SHA512", 128)]
    [InlineData("SHA1", 40)]
    [InlineData("MD5", 32)]
    public async Task GetFileHash_AllSupportedAlgorithms_MatchIndependentHash(string name, int hexLength)
    {
        string path = Path.Combine(_testDirectory, "hash.dat");
        byte[] content = [1, 2, 3, 0, 255];
        await File.WriteAllBytesAsync(path, content, TestContext.Current.CancellationToken);
        HashAlgorithmName algorithm = new(name);
        using IncrementalHash hash = IncrementalHash.CreateHash(algorithm);
        hash.AppendData(content);
        string expected = Convert.ToHexString(hash.GetHashAndReset());
        Assert.Equal(hexLength, expected.Length);
        Assert.Equal(expected, FileHelper.GetFileHash(path, algorithm));
        Assert.Equal(expected, await FileHelper.GetFileHashAsync(path, algorithm, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetFileHash_UnsupportedAlgorithm_Throws()
    {
        string path = Path.Combine(_testDirectory, "hash.dat");
        File.WriteAllText(path, "");
        Assert.Throws<NotSupportedException>(() => FileHelper.GetFileHash(path, new("unsupported")));
        await Assert.ThrowsAsync<NotSupportedException>(() => FileHelper.GetFileHashAsync(path, new("unsupported"), cancellationToken: TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(1125899906842624, "1 PB")]
    [InlineData(1152921504606846976, "1 EB")]
    [InlineData(long.MaxValue, "8 EB")]
    public void GetReadableFileSize_LargeValues_UsesCompleteUnits(long bytes, string expected)
        => Assert.Equal(expected, FileHelper.GetReadableFileSize(bytes));

    [Fact]
    public void GetReadableFileSize_NegativeOrDifferentCulture_ValidatesAndFormatsConsistently()
    {
        Assert.Throws<ArgumentOutOfRangeException>("bytes", () => FileHelper.GetReadableFileSize(-1));
        System.Globalization.CultureInfo original = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new("fr-FR");
            Assert.Equal("1.5 KB", FileHelper.GetReadableFileSize(1536));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = original; }
    }

    [Fact]
    public async Task CopyFileAsync_ExistingDestination_RespectsOverwrite()
    {
        string source = Path.Combine(_testDirectory, "source");
        string destination = Path.Combine(_testDirectory, "destination");
        File.WriteAllText(source, "new");
        File.WriteAllText(destination, "old");
        await Assert.ThrowsAsync<IOException>(() => FileHelper.CopyFileAsync(source, destination, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal("old", File.ReadAllText(destination));
        await FileHelper.CopyFileAsync(source, destination, overwrite: true, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("new", File.ReadAllText(destination));
    }

    [Fact]
    public async Task CopyFileAsync_ConcurrentWriters_OnlyOneCreatesDestination()
    {
        string source = Path.Combine(_testDirectory, "source");
        string destination = Path.Combine(_testDirectory, "destination");
        File.WriteAllText(source, new string('x', 100000));
        async Task<bool> Copy()
        {
            try { await FileHelper.CopyFileAsync(source, destination, cancellationToken: TestContext.Current.CancellationToken); return true; }
            catch (IOException) { return false; }
        }
        bool[] results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Copy()));
        Assert.Single(results, success => success);
        Assert.Equal(File.ReadAllBytes(source), File.ReadAllBytes(destination));
    }

    [Fact]
    public async Task AsyncFileOperations_PreCancelled_DoNotTouchFiles()
    {
        string source = Path.Combine(_testDirectory, "source");
        string destination = Path.Combine(_testDirectory, "destination");
        File.WriteAllText(source, "source");
        File.WriteAllText(destination, "keep");
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        CancellationToken token = cancellation.Token;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FileHelper.CopyFileAsync(source, destination, true, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FileHelper.GetFileHashAsync(source, cancellationToken: token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FileHelper.GetFileSizeAsync(source, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FileHelper.DeleteFileAsync(source, cancellationToken: token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FileHelper.CompareFilesAsync(source, destination, token));
        Assert.Equal("source", File.ReadAllText(source));
        Assert.Equal("keep", File.ReadAllText(destination));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(4096, false)]
    [InlineData(10001, false)]
    [InlineData(10001, true)]
    public async Task CompareFiles_EmptyAndMultipleBuffers_AgreeSynchronouslyAndAsynchronously(int size, bool change)
    {
        string first = Path.Combine(_testDirectory, "first");
        string second = Path.Combine(_testDirectory, "second");
        byte[] content = Enumerable.Range(0, size).Select(i => (byte)i).ToArray();
        File.WriteAllBytes(first, content);
        if (change) content[^1]++;
        File.WriteAllBytes(second, content);
        Assert.Equal(!change, FileHelper.CompareFiles(first, second));
        Assert.Equal(!change, await FileHelper.CompareFilesAsync(first, second, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CompareFilesAsync_DifferentLengths_ReturnsFalse()
    {
        string first = Path.Combine(_testDirectory, "first");
        string second = Path.Combine(_testDirectory, "second");
        File.WriteAllText(first, "a");
        File.WriteAllText(second, "ab");
        Assert.False(await FileHelper.CompareFilesAsync(first, second, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public async Task DeleteFile_InvalidRetryArguments_Throw(int count, int delay)
    {
        string path = Path.Combine(_testDirectory, "missing");
        Assert.Throws<ArgumentOutOfRangeException>(() => FileHelper.DeleteFile(path, count, delay));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => FileHelper.DeleteFileAsync(path, count, delay, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteFile_WindowsLockedFile_ReturnsFalseAfterRetries()
    {
        if (!OperatingSystem.IsWindows()) return;
        string path = Path.Combine(_testDirectory, "locked");
        File.WriteAllText(path, "locked");
        using FileStream handle = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.False(FileHelper.DeleteFile(path, retryCount: 1, retryDelayMilliseconds: 0));
        Assert.False(await FileHelper.DeleteFileAsync(path, retryCount: 1, retryDelayMilliseconds: 0, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteFile_UnixDeniedDirectory_ReturnsFalse()
    {
        if (OperatingSystem.IsWindows()) return;
        string directory = Path.Combine(_testDirectory, "denied");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "file");
        File.WriteAllText(path, "content");
        UnixFileMode original = File.GetUnixFileMode(directory);
        try
        {
            File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserExecute);
            Assert.False(FileHelper.DeleteFile(path));
            Assert.False(await FileHelper.DeleteFileAsync(path, cancellationToken: TestContext.Current.CancellationToken));
        }
        finally { File.SetUnixFileMode(directory, original); }
    }

    [Fact]
    public void IsFileLocked_UnixDeniedRead_ReturnsTrue()
    {
        if (OperatingSystem.IsWindows()) return;
        string path = Path.Combine(_testDirectory, "denied");
        File.WriteAllText(path, "content");
        UnixFileMode original = File.GetUnixFileMode(path);
        try
        {
            File.SetUnixFileMode(path, UnixFileMode.None);
            Assert.True(FileHelper.IsFileLocked(path));
        }
        finally { File.SetUnixFileMode(path, original); }
    }

    [Fact]
    public async Task AsyncFileOperations_NullArguments_Throw()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => FileHelper.CopyFileAsync("source", null!, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => FileHelper.DeleteFileAsync(null!, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => FileHelper.CompareFilesAsync(null!, "second", cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => FileHelper.CompareFilesAsync("first", null!, cancellationToken: TestContext.Current.CancellationToken));
    }
    private readonly string _testDirectory;

    public FileHelperTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"FileHelperTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, true);

        GC.SuppressFinalize(this);
    }

    #region GetUniqueFileName Tests

    [Fact]
    public void GetUniqueFileName_ReturnsOriginalName_WhenFileDoesNotExist()
    {
        // Arrange
        var fileName = "test.txt";

        // Act
        var result = FileHelper.GetUniqueFileName(_testDirectory, fileName);

        // Assert
        Assert.Equal(fileName, result);
    }

    [Fact]
    public void GetUniqueFileName_ReturnsUniqueNameWithCounter_WhenFileExists()
    {
        // Arrange
        var fileName = "test.txt";
        var filePath = Path.Combine(_testDirectory, fileName);
        File.WriteAllText(filePath, "content");

        // Act
        var result = FileHelper.GetUniqueFileName(_testDirectory, fileName);

        // Assert
        Assert.Equal("test_1.txt", result);
    }

    [Fact]
    public void GetUniqueFileName_IncrementsCounter_ForMultipleExistingFiles()
    {
        // Arrange
        var fileName = "test.txt";
        File.WriteAllText(Path.Combine(_testDirectory, "test.txt"), "content");
        File.WriteAllText(Path.Combine(_testDirectory, "test_1.txt"), "content");
        File.WriteAllText(Path.Combine(_testDirectory, "test_2.txt"), "content");

        // Act
        var result = FileHelper.GetUniqueFileName(_testDirectory, fileName);

        // Assert
        Assert.Equal("test_3.txt", result);
    }

    [Fact]
    public void GetUniqueFileName_ThrowsArgumentException_WhenDirectoryIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileHelper.GetUniqueFileName(null!, "test.txt"));
    }

    [Fact]
    public void GetUniqueFileName_ThrowsArgumentException_WhenFileNameIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileHelper.GetUniqueFileName(_testDirectory, null!));
    }

    #endregion

    #region GetFileSize Tests

    [Fact]
    public void GetFileSize_ReturnsCorrectSize()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var content = "Hello World!";
        File.WriteAllText(filePath, content);

        // Act
        var size = FileHelper.GetFileSize(filePath);

        // Assert
        Assert.Equal(new FileInfo(filePath).Length, size);
    }

    [Fact]
    public void GetFileSize_ThrowsArgumentException_WhenPathIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileHelper.GetFileSize(null!));
    }

    [Fact]
    public async Task GetFileSizeAsync_ReturnsCorrectSize()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var content = "Hello World!";
        File.WriteAllText(filePath, content);

        // Act
        var size = await FileHelper.GetFileSizeAsync(filePath, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(new FileInfo(filePath).Length, size);
    }

    [Fact]
    public async Task GetFileSizeAsync_ThrowsArgumentException_WhenPathIsNull()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => FileHelper.GetFileSizeAsync(null!, TestContext.Current.CancellationToken));
    }

    #endregion

    #region GetFileHash Tests

    [Fact]
    public void GetFileHash_ReturnsCorrectSHA256Hash()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var content = "Hello World!";
        File.WriteAllText(filePath, content);

        // Act
        var hash = FileHelper.GetFileHash(filePath);

        // Assert
        Assert.NotEmpty(hash);
        Assert.Matches("^[0-9A-F]+$", hash);
    }

    [Fact]
    public void GetFileHash_ReturnsSameHashForSameContent()
    {
        // Arrange
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        var file2 = Path.Combine(_testDirectory, "file2.txt");
        var content = "Same content";
        File.WriteAllText(file1, content);
        File.WriteAllText(file2, content);

        // Act
        var hash1 = FileHelper.GetFileHash(file1);
        var hash2 = FileHelper.GetFileHash(file2);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetFileHash_ReturnsDifferentHashForDifferentContent()
    {
        // Arrange
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        var file2 = Path.Combine(_testDirectory, "file2.txt");
        File.WriteAllText(file1, "Content 1");
        File.WriteAllText(file2, "Content 2");

        // Act
        var hash1 = FileHelper.GetFileHash(file1);
        var hash2 = FileHelper.GetFileHash(file2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GetFileHash_ThrowsArgumentException_WhenPathIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileHelper.GetFileHash(null!));
    }

    [Fact]
    public async Task GetFileHashAsync_ReturnsCorrectHash()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "Hello World!");

        // Act
        var hash = await FileHelper.GetFileHashAsync(filePath, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(hash);
        Assert.Matches("^[0-9A-F]+$", hash);
    }

    [Fact]
    public async Task GetFileHashAsync_ThrowsArgumentException_WhenPathIsNull()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => FileHelper.GetFileHashAsync(null!, cancellationToken: TestContext.Current.CancellationToken));
    }

    #endregion

    #region IsFileLocked Tests

    [Fact]
    public void IsFileLocked_ReturnsFalse_WhenFileIsNotLocked()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "content");

        // Act
        var isLocked = FileHelper.IsFileLocked(filePath);

        // Assert
        Assert.False(isLocked);
    }

    [Fact]
    public void IsFileLocked_ReturnsTrue_WhenFileIsLocked()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "content");

        using var stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        // Act
        var isLocked = FileHelper.IsFileLocked(filePath);

        // Assert
        Assert.True(isLocked);
    }

    [Fact]
    public void IsFileLocked_ReturnsFalse_WhenFileDoesNotExist()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act
        var isLocked = FileHelper.IsFileLocked(filePath);

        // Assert
        Assert.False(isLocked);
    }

    [Fact]
    public void IsFileLocked_ThrowsArgumentException_WhenPathIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileHelper.IsFileLocked(null!));
    }

    #endregion

    #region EnsureDirectoryExists Tests

    [Fact]
    public void EnsureDirectoryExists_CreatesDirectory_WhenItDoesNotExist()
    {
        // Arrange
        var newDir = Path.Combine(_testDirectory, "newdir");

        // Act
        FileHelper.EnsureDirectoryExists(newDir);

        // Assert
        Assert.True(Directory.Exists(newDir));
    }

    [Fact]
    public void EnsureDirectoryExists_DoesNotThrow_WhenDirectoryAlreadyExists()
    {
        // Arrange
        var dir = Path.Combine(_testDirectory, "existingdir");
        Directory.CreateDirectory(dir);

        // Act & Assert
        var exception = Record.Exception(() => FileHelper.EnsureDirectoryExists(dir));
        Assert.Null(exception);
    }

    [Fact]
    public void EnsureDirectoryExists_ThrowsArgumentException_WhenPathIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileHelper.EnsureDirectoryExists(null!));
    }

    #endregion

    #region GetFilesRecursive Tests

    [Fact]
    public void GetFilesRecursive_ReturnsAllFiles_InDirectoryTree()
    {
        // Arrange
        var subDir1 = Path.Combine(_testDirectory, "sub1");
        var subDir2 = Path.Combine(_testDirectory, "sub1", "sub2");
        Directory.CreateDirectory(subDir1);
        Directory.CreateDirectory(subDir2);

        File.WriteAllText(Path.Combine(_testDirectory, "file1.txt"), "content");
        File.WriteAllText(Path.Combine(subDir1, "file2.txt"), "content");
        File.WriteAllText(Path.Combine(subDir2, "file3.txt"), "content");

        // Act
        var files = FileHelper.GetFilesRecursive(_testDirectory).ToList();

        // Assert
        Assert.Equal(3, files.Count);
    }

    [Fact]
    public void GetFilesRecursive_FiltersFilesByPattern()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDirectory, "file1.txt"), "content");
        File.WriteAllText(Path.Combine(_testDirectory, "file2.csv"), "content");
        File.WriteAllText(Path.Combine(_testDirectory, "file3.txt"), "content");

        // Act
        var files = FileHelper.GetFilesRecursive(_testDirectory, "*.txt").ToList();

        // Assert
        Assert.Equal(2, files.Count);
        Assert.All(files, f => Assert.EndsWith(".txt", f));
    }

    [Fact]
    public void GetFilesRecursive_ThrowsArgumentException_WhenDirectoryIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileHelper.GetFilesRecursive(null!));
    }

    #endregion

    #region GetRelativePath Tests

    [Fact]
    public void GetRelativePath_ReturnsCorrectRelativePath()
    {
        // Arrange
        var from = Path.Combine(_testDirectory, "folder1");
        var to = Path.Combine(_testDirectory, "folder2", "file.txt");

        // Act
        var relativePath = FileHelper.GetRelativePath(from, to);

        // Assert
        Assert.NotEmpty(relativePath);
    }

    [Fact]
    public void GetRelativePath_ThrowsArgumentException_WhenFromPathIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileHelper.GetRelativePath(null!, "somepath"));
    }

    [Fact]
    public void GetRelativePath_ThrowsArgumentException_WhenToPathIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileHelper.GetRelativePath("somepath", null!));
    }

    #endregion

    #region SanitizeFileName Tests

    [Fact]
    public void SanitizeFileName_RemovesInvalidCharacters()
    {
        // Arrange
        var invalidChars = Path.GetInvalidFileNameChars();
        var fileName = $"test{invalidChars[0]}file{invalidChars[^1]}name.txt";

        // Act
        var sanitized = FileHelper.SanitizeFileName(fileName);

        // Assert
        Assert.Equal("testfilename.txt", sanitized);
    }

    [Fact]
    public void SanitizeFileName_ReturnsEmpty_WhenInputIsNull()
    {
        // Arrange & Act
        var result = FileHelper.SanitizeFileName(null!);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void SanitizeFileName_ReturnsEmpty_WhenInputIsWhiteSpace()
    {
        // Arrange & Act
        var result = FileHelper.SanitizeFileName("   ");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void SanitizeFileName_KeepsValidCharacters()
    {
        // Arrange
        var fileName = "valid_file-name.txt";

        // Act
        var sanitized = FileHelper.SanitizeFileName(fileName);

        // Assert
        Assert.Equal(fileName, sanitized);
    }

    #endregion

    #region GetReadableFileSize Tests

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    [InlineData(1099511627776, "1 TB")]
    public void GetReadableFileSize_ReturnsCorrectFormat(long bytes, string expected)
    {
        // Act
        var result = FileHelper.GetReadableFileSize(bytes);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region CopyFile Tests

    [Fact]
    public void CopyFile_CopiesFileSuccessfully()
    {
        // Arrange
        var source = Path.Combine(_testDirectory, "source.txt");
        var destination = Path.Combine(_testDirectory, "destination.txt");
        File.WriteAllText(source, "content");

        // Act
        FileHelper.CopyFile(source, destination);

        // Assert
        Assert.True(File.Exists(destination));
        Assert.Equal("content", File.ReadAllText(destination));
    }

    [Fact]
    public void CopyFile_OverwritesExistingFile_WhenOverwriteIsTrue()
    {
        // Arrange
        var source = Path.Combine(_testDirectory, "source.txt");
        var destination = Path.Combine(_testDirectory, "destination.txt");
        File.WriteAllText(source, "new content");
        File.WriteAllText(destination, "old content");

        // Act
        FileHelper.CopyFile(source, destination, overwrite: true);

        // Assert
        Assert.Equal("new content", File.ReadAllText(destination));
    }

    [Fact]
    public void CopyFile_ThrowsArgumentException_WhenSourceIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileHelper.CopyFile(null!, "dest"));
    }

    [Fact]
    public void CopyFile_ThrowsArgumentException_WhenDestinationIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileHelper.CopyFile("source", null!));
    }

    [Fact]
    public async Task CopyFileAsync_CopiesFileSuccessfully()
    {
        // Arrange
        var source = Path.Combine(_testDirectory, "source.txt");
        var destination = Path.Combine(_testDirectory, "destination.txt");
        File.WriteAllText(source, "content");

        // Act
        await FileHelper.CopyFileAsync(source, destination, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.True(File.Exists(destination));
        Assert.Equal("content", File.ReadAllText(destination));
    }

    [Fact]
    public async Task CopyFileAsync_ThrowsArgumentException_WhenSourceIsNull()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => FileHelper.CopyFileAsync(null!, "dest", cancellationToken: TestContext.Current.CancellationToken));
    }

    #endregion

    #region MoveFile Tests

    [Fact]
    public void MoveFile_MovesFileSuccessfully()
    {
        // Arrange
        var source = Path.Combine(_testDirectory, "source.txt");
        var destination = Path.Combine(_testDirectory, "destination.txt");
        File.WriteAllText(source, "content");

        // Act
        FileHelper.MoveFile(source, destination);

        // Assert
        Assert.False(File.Exists(source));
        Assert.True(File.Exists(destination));
        Assert.Equal("content", File.ReadAllText(destination));
    }

    [Fact]
    public void MoveFile_ThrowsArgumentException_WhenSourceIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileHelper.MoveFile(null!, "dest"));
    }

    [Fact]
    public void MoveFile_ThrowsArgumentException_WhenDestinationIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileHelper.MoveFile("source", null!));
    }

    #endregion

    #region DeleteFile Tests

    [Fact]
    public void DeleteFile_DeletesFileSuccessfully()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "content");

        // Act
        var result = FileHelper.DeleteFile(filePath);

        // Assert
        Assert.True(result);
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public void DeleteFile_ReturnsFalse_WhenFileDoesNotExist()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act
        var result = FileHelper.DeleteFile(filePath);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void DeleteFile_ThrowsArgumentException_WhenPathIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileHelper.DeleteFile(null!));
    }

    [Fact]
    public async Task DeleteFileAsync_DeletesFileSuccessfully()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "content");

        // Act
        var result = await FileHelper.DeleteFileAsync(filePath, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteFileAsync_ReturnsFalse_WhenFileDoesNotExist()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act
        var result = await FileHelper.DeleteFileAsync(filePath, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetLastModified Tests

    [Fact]
    public void GetLastModified_ReturnsCorrectDate()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "content");
        var expectedDate = File.GetLastWriteTime(filePath);

        // Act
        var lastModified = FileHelper.GetLastModified(filePath);

        // Assert
        Assert.Equal(expectedDate, lastModified);
    }

    [Fact]
    public void GetLastModified_ThrowsArgumentException_WhenPathIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileHelper.GetLastModified(null!));
    }

    #endregion

    #region GetFileAge Tests

    [Fact]
    public void GetFileAge_ReturnsCorrectAge()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "content");
        Thread.Sleep(100);

        // Act
        var age = FileHelper.GetFileAge(filePath);

        // Assert
        Assert.True(age.TotalMilliseconds >= 100);
    }

    [Fact]
    public void GetFileAge_ThrowsArgumentException_WhenPathIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileHelper.GetFileAge(null!));
    }

    #endregion

    #region CompareFiles Tests

    [Fact]
    public void CompareFiles_ReturnsTrue_ForIdenticalFiles()
    {
        // Arrange
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        var file2 = Path.Combine(_testDirectory, "file2.txt");
        var content = "Same content";
        File.WriteAllText(file1, content);
        File.WriteAllText(file2, content);

        // Act
        var result = FileHelper.CompareFiles(file1, file2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CompareFiles_ReturnsFalse_ForDifferentFiles()
    {
        // Arrange
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        var file2 = Path.Combine(_testDirectory, "file2.txt");
        File.WriteAllText(file1, "Content 1");
        File.WriteAllText(file2, "Content 2");

        // Act
        var result = FileHelper.CompareFiles(file1, file2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CompareFiles_ReturnsFalse_ForFilesDifferentSize()
    {
        // Arrange
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        var file2 = Path.Combine(_testDirectory, "file2.txt");
        File.WriteAllText(file1, "Short");
        File.WriteAllText(file2, "Much longer content");

        // Act
        var result = FileHelper.CompareFiles(file1, file2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CompareFiles_ThrowsArgumentException_WhenPath1IsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileHelper.CompareFiles(null!, "file2"));
    }

    [Fact]
    public void CompareFiles_ThrowsArgumentException_WhenPath2IsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileHelper.CompareFiles("file1", null!));
    }

    [Fact]
    public async Task CompareFilesAsync_ReturnsTrue_ForIdenticalFiles()
    {
        // Arrange
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        var file2 = Path.Combine(_testDirectory, "file2.txt");
        var content = "Same content";
        File.WriteAllText(file1, content);
        File.WriteAllText(file2, content);

        // Act
        var result = await FileHelper.CompareFilesAsync(file1, file2, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CompareFilesAsync_ReturnsFalse_ForDifferentFiles()
    {
        // Arrange
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        var file2 = Path.Combine(_testDirectory, "file2.txt");
        File.WriteAllText(file1, "Content 1");
        File.WriteAllText(file2, "Content 2");

        // Act
        var result = await FileHelper.CompareFilesAsync(file1, file2, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    #endregion
}
