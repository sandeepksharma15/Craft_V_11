namespace Craft.Domain.Abstractions;

/// <summary>Defines a contract for entities that support versioning for tracking changes.</summary>
public interface IHasVersion
{
    /// <summary>
    /// The name of the database column for the Version property.
    /// </summary>
    public const string ColumnName = "Version";

    public long Version { get; set; }

    /// <summary>
    /// Decrements the version number if it is greater than zero.
    /// </summary>
    public void DecrementVersion()
    {
        if (Version > 0)
            Version--;
    }

    public long GetVersion() => Version;

    /// <summary>
    /// Increments the version number.
    /// </summary>
    public void IncrementVersion() => Version++;

    public void SetVersion(long version) => Version = version;
}
