using System.Globalization;
using System.Security.Cryptography;

namespace Craft.Utilities.Helpers;

public static class FileHelper
{
    #region Private Fields

    private const int BytesInKilobyte = 1024;

    #endregion Private Fields

    #region Private Methods

    private static HashAlgorithm CreateHashAlgorithm(HashAlgorithmName algorithmName)
    {
        if (algorithmName == HashAlgorithmName.SHA256)
            return SHA256.Create();

        if (algorithmName == HashAlgorithmName.SHA384)
            return SHA384.Create();

        if (algorithmName == HashAlgorithmName.SHA512)
            return SHA512.Create();

        if (algorithmName == HashAlgorithmName.SHA1)
            return SHA1.Create();

        if (algorithmName == HashAlgorithmName.MD5)
            return MD5.Create();

        throw new NotSupportedException($"Hash algorithm '{algorithmName.Name}' is not supported.");
    }

    #endregion Private Methods

    #region Public Methods

    /// <summary>
    /// Compares two files to determine if they have identical content.
    /// </summary>
    /// <param name="path1"> The path to the first file. </param>
    /// <param name="path2"> The path to the second file. </param>
    /// <returns> True if the files have identical content; otherwise, false. </returns>
    /// <exception cref="ArgumentException"> Thrown when either path is null or empty. </exception>
    public static bool CompareFiles(string path1, string path2)
    {
        ArgumentException.ThrowIfNullOrEmpty(path1);
        ArgumentException.ThrowIfNullOrEmpty(path2);

        using var stream1 = File.OpenRead(path1);
        using var stream2 = File.OpenRead(path2);

        const int bufferSize = 4096;
        var buffer1 = new byte[bufferSize];
        var buffer2 = new byte[bufferSize];

        int bytesRead1, bytesRead2;
        do
        {
            bytesRead1 = stream1.ReadAtLeast(buffer1, bufferSize, throwOnEndOfStream: false);
            bytesRead2 = stream2.ReadAtLeast(buffer2, bufferSize, throwOnEndOfStream: false);

            if (bytesRead1 != bytesRead2)
                return false;

            if (!buffer1.AsSpan(0, bytesRead1).SequenceEqual(buffer2.AsSpan(0, bytesRead2)))
                return false;
        } while (bytesRead1 > 0);

        return true;
    }

    /// <summary>
    /// Asynchronously compares two files to determine if they have identical content.
    /// </summary>
    /// <param name="path1">             The path to the first file. </param>
    /// <param name="path2">             The path to the second file. </param>
    /// <param name="cancellationToken"> The cancellation token. </param>
    /// <returns> True if the files have identical content; otherwise, false. </returns>
    /// <exception cref="ArgumentException"> Thrown when either path is null or empty. </exception>
    public static async Task<bool> CompareFilesAsync(string path1, string path2, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path1);
        ArgumentException.ThrowIfNullOrEmpty(path2);

        cancellationToken.ThrowIfCancellationRequested();

        using var stream1 = new FileStream(path1, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        using var stream2 = new FileStream(path2, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);

        const int bufferSize = 4096;
        var buffer1 = new byte[bufferSize];
        var buffer2 = new byte[bufferSize];

        int bytesRead1, bytesRead2;
        do
        {
            bytesRead1 = await stream1.ReadAtLeastAsync(buffer1, bufferSize, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);
            bytesRead2 = await stream2.ReadAtLeastAsync(buffer2, bufferSize, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);

            if (bytesRead1 != bytesRead2)
                return false;

            if (!buffer1.AsSpan(0, bytesRead1).SequenceEqual(buffer2.AsSpan(0, bytesRead2)))
                return false;
        } while (bytesRead1 > 0);

        return true;
    }

    /// <summary>
    /// Copies a file to a destination path with optional overwrite.
    /// </summary>
    /// <param name="sourcePath">      The source file path. </param>
    /// <param name="destinationPath"> The destination file path. </param>
    /// <param name="overwrite">      
    /// Whether to overwrite the destination file if it exists.
    /// </param>
    /// <exception cref="ArgumentException"> Thrown when either path is null or empty. </exception>
    public static void CopyFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourcePath);
        ArgumentException.ThrowIfNullOrEmpty(destinationPath);

        File.Copy(sourcePath, destinationPath, overwrite);
    }

    /// <summary>
    /// Asynchronously copies a file to a destination path with optional overwrite.
    /// </summary>
    /// <param name="sourcePath">        The source file path. </param>
    /// <param name="destinationPath">   The destination file path. </param>
    /// <param name="overwrite">        
    /// Whether to overwrite the destination file if it exists.
    /// </param>
    /// <param name="cancellationToken"> The cancellation token. </param>
    /// <exception cref="ArgumentException"> Thrown when either path is null or empty. </exception>
    public static async Task CopyFileAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourcePath);
        ArgumentException.ThrowIfNullOrEmpty(destinationPath);

        cancellationToken.ThrowIfCancellationRequested();

        using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        using var destinationStream = new FileStream(destinationPath, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);

        await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Safely deletes a file with optional retry logic for locked files.
    /// </summary>
    /// <param name="path">                   The path to the file to delete. </param>
    /// <param name="retryCount">            
    /// The number of retry attempts if the file is locked.
    /// </param>
    /// <param name="retryDelayMilliseconds"> The delay in milliseconds between retry attempts. </param>
    /// <returns>
    /// True if the file was deleted; false if it does not exist or could not be deleted after retries.
    /// </returns>
    /// <exception cref="ArgumentException"> Thrown when the path is null or empty. </exception>
    public static bool DeleteFile(string path, int retryCount = 3, int retryDelayMilliseconds = 100)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentOutOfRangeException.ThrowIfNegative(retryCount);
        ArgumentOutOfRangeException.ThrowIfNegative(retryDelayMilliseconds);

        if (!File.Exists(path))
            return false;

        for (int attempt = 0; ; attempt++)
        {
            try
            {
                File.Delete(path);
                return true;
            }
            catch (IOException)
            {
                if (attempt == retryCount)
                    return false;
                Thread.Sleep(retryDelayMilliseconds);
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Asynchronously and safely deletes a file with optional retry logic for locked files.
    /// </summary>
    /// <param name="path">                   The path to the file to delete. </param>
    /// <param name="retryCount">            
    /// The number of retry attempts if the file is locked.
    /// </param>
    /// <param name="retryDelayMilliseconds"> The delay in milliseconds between retry attempts. </param>
    /// <param name="cancellationToken">      The cancellation token. </param>
    /// <returns>
    /// True if the file was deleted; false if it does not exist or could not be deleted after retries.
    /// </returns>
    /// <exception cref="ArgumentException"> Thrown when the path is null or empty. </exception>
    public static async Task<bool> DeleteFileAsync(string path, int retryCount = 3, int retryDelayMilliseconds = 100, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentOutOfRangeException.ThrowIfNegative(retryCount);
        ArgumentOutOfRangeException.ThrowIfNegative(retryDelayMilliseconds);
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(path))
            return false;

        for (int attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                File.Delete(path);
                return true;
            }
            catch (IOException)
            {
                if (attempt == retryCount)
                    return false;
                await Task.Delay(retryDelayMilliseconds, cancellationToken).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Ensures that a directory exists, creating it if necessary.
    /// </summary>
    /// <param name="path"> The directory path to ensure exists. </param>
    /// <exception cref="ArgumentException"> Thrown when the path is null or empty. </exception>
    public static void EnsureDirectoryExists(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        Directory.CreateDirectory(path);
    }

    /// <summary>
    /// Gets the age of a file (time elapsed since last modification).
    /// </summary>
    /// <param name="path"> The path to the file. </param>
    /// <returns> The time elapsed since the file was last modified. </returns>
    /// <exception cref="ArgumentException"> Thrown when the path is null or empty. </exception>
    public static TimeSpan GetFileAge(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        return DateTime.UtcNow - File.GetLastWriteTimeUtc(path);
    }

    /// <summary>
    /// Computes the hash of a file using the specified hash algorithm.
    /// </summary>
    /// <param name="path">      The path to the file. </param>
    /// <param name="algorithm"> The hash algorithm to use. Defaults to SHA256. </param>
    /// <returns> A hexadecimal string representation of the computed hash. </returns>
    /// <exception cref="ArgumentException"> Thrown when the path is null or empty. </exception>
    /// <exception cref="NotSupportedException"> Thrown when the hash algorithm is not supported. </exception>
    public static string GetFileHash(string path, HashAlgorithmName algorithm = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var algorithmName = algorithm == default ? HashAlgorithmName.SHA256 : algorithm;
        using var stream = File.OpenRead(path);
        using var hashAlgorithm = CreateHashAlgorithm(algorithmName);

        var hash = hashAlgorithm.ComputeHash(stream);

        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Asynchronously computes the hash of a file using the specified hash algorithm.
    /// </summary>
    /// <param name="path">              The path to the file. </param>
    /// <param name="algorithm">         The hash algorithm to use. Defaults to SHA256. </param>
    /// <param name="cancellationToken"> The cancellation token. </param>
    /// <returns> A hexadecimal string representation of the computed hash. </returns>
    /// <exception cref="ArgumentException"> Thrown when the path is null or empty. </exception>
    /// <exception cref="NotSupportedException"> Thrown when the hash algorithm is not supported. </exception>
    public static async Task<string> GetFileHashAsync(string path, HashAlgorithmName algorithm = default, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var algorithmName = algorithm == default ? HashAlgorithmName.SHA256 : algorithm;

        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        using var hashAlgorithm = CreateHashAlgorithm(algorithmName);

        var hash = await hashAlgorithm.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);

        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Gets the size of a file in bytes.
    /// </summary>
    /// <param name="path"> The path to the file. </param>
    /// <returns> The size of the file in bytes. </returns>
    /// <exception cref="ArgumentException"> Thrown when the path is null or empty. </exception>
    public static long GetFileSize(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        return new FileInfo(path).Length;
    }

    /// <summary>
    /// Asynchronously gets the size of a file in bytes.
    /// </summary>
    /// <param name="path">              The path to the file. </param>
    /// <param name="cancellationToken"> The cancellation token. </param>
    /// <returns> The size of the file in bytes. </returns>
    /// <exception cref="ArgumentException"> Thrown when the path is null or empty. </exception>
    public static Task<long> GetFileSizeAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new FileInfo(path).Length);
    }

    /// <summary>
    /// Gets all files recursively from a directory matching the specified pattern.
    /// </summary>
    /// <param name="directory"> The directory to search. </param>
    /// <param name="pattern">  
    /// The search pattern to match against file names. Defaults to "*.*".
    /// </param>
    /// <returns> An enumerable collection of file paths. </returns>
    /// <exception cref="ArgumentException"> Thrown when the directory is null or empty. </exception>
    public static IEnumerable<string> GetFilesRecursive(string directory, string pattern = "*.*")
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);

        return Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories);
    }

    /// <summary>
    /// Gets the last modified date and time of a file.
    /// </summary>
    /// <param name="path"> The path to the file. </param>
    /// <returns> The last write time of the file. </returns>
    /// <exception cref="ArgumentException"> Thrown when the path is null or empty. </exception>
    public static DateTime GetLastModified(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        return File.GetLastWriteTime(path);
    }

    /// <summary>
    /// Converts a file size in bytes to a human-readable string (e.g., "1.5 MB").
    /// </summary>
    /// <param name="bytes"> The size in bytes. </param>
    /// <returns> A formatted string representing the file size with appropriate unit. </returns>
    public static string GetReadableFileSize(long bytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);
        string[] sizes = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];
        double len = bytes;
        int order = 0;

        while (len >= BytesInKilobyte && order < sizes.Length - 1)
        {
            order++;
            len /= BytesInKilobyte;
        }

        return string.Create(CultureInfo.InvariantCulture, $"{len:0.##} {sizes[order]}");
    }

    /// <summary>
    /// Gets the relative path from one path to another.
    /// </summary>
    /// <param name="fromPath"> The source path. </param>
    /// <param name="toPath">   The destination path. </param>
    /// <returns> The relative path from the source to the destination. </returns>
    /// <exception cref="ArgumentException"> Thrown when either path is null or empty. </exception>
    public static string GetRelativePath(string fromPath, string toPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(fromPath);
        ArgumentException.ThrowIfNullOrEmpty(toPath);

        return Path.GetRelativePath(fromPath, toPath);
    }

    /// <summary>
    /// Generates a unique file name by appending a counter if the file already exists in the
    /// specified directory.
    /// </summary>
    /// <param name="directory"> The directory to check for existing files. </param>
    /// <param name="fileName">  The original file name. </param>
    /// <returns>
    /// An available file name. This check does not reserve the name; use CreateNew when creating
    /// the file.
    /// </returns>
    public static string GetUniqueFileName(string directory, string fileName)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        if (Path.GetFileName(fileName) != fileName || fileName is "." or "..")
            throw new ArgumentException("A file name without a directory is required.", nameof(fileName));

        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var uniqueName = fileName;
        var counter = 1;

        while (Path.Exists(Path.Combine(directory, uniqueName)))
        {
            uniqueName = $"{name}_{counter}{extension}";
            counter++;
        }

        return uniqueName;
    }

    /// <summary>
    /// Determines whether a file is locked by attempting to open it exclusively.
    /// </summary>
    /// <param name="path"> The path to the file. </param>
    /// <returns> True if the file is locked or inaccessible; otherwise, false. </returns>
    /// <exception cref="ArgumentException"> Thrown when the path is null or empty. </exception>
    public static bool IsFileLocked(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (!File.Exists(path))
            return false;

        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }

    /// <summary>
    /// Moves a file to a destination path with optional overwrite.
    /// </summary>
    /// <param name="sourcePath">      The source file path. </param>
    /// <param name="destinationPath"> The destination file path. </param>
    /// <param name="overwrite">      
    /// Whether to overwrite the destination file if it exists.
    /// </param>
    /// <exception cref="ArgumentException"> Thrown when either path is null or empty. </exception>
    public static void MoveFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourcePath);
        ArgumentException.ThrowIfNullOrEmpty(destinationPath);

        File.Move(sourcePath, destinationPath, overwrite);
    }

    /// <summary>
    /// Removes invalid characters from a file name.
    /// </summary>
    /// <param name="fileName"> The file name to sanitize. </param>
    /// <returns> A sanitized file name with all invalid characters removed. </returns>
    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Where(c => !invalidChars.Contains(c)));
    }

    #endregion Public Methods
}
