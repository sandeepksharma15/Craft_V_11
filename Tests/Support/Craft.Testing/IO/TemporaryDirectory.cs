namespace Craft.Testing.IO;

/// <summary>Owns a unique temporary directory. Dispose after closing all files to remove its contents.</summary>
public sealed class TemporaryDirectory : IDisposable
{
    public string DirectoryPath { get; } = Directory.CreateTempSubdirectory("Craft.Tests.").FullName;

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, recursive: true);
    }
}
