namespace Craft.Domain.Abstractions;

/// <summary>Defines a contract for entities that can be activated or deactivated.</summary>
public interface IHasActive
{
    /// <summary>
    /// The name of the database column for the IsActive property.
    /// </summary>
    public const string ColumnName = "IsActive";

    bool IsActive { get; set; }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void SetActive(bool isActive) => IsActive = isActive;

    public void ToggleActive() => IsActive = !IsActive;
}
