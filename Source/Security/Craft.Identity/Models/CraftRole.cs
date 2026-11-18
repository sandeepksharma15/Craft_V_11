using Craft.Identity.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace Craft.Identity.Models;

public class CraftRole<TKey> : IdentityRole<TKey>, ICraftRole<TKey>
    where TKey : IEquatable<TKey>
{
    #region Public Constructors

    public CraftRole() { }

    public CraftRole(string? name, string? description = null)
    {
        Name = name;
        Description = description;
        IsActive = true;
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    #endregion Public Constructors

    #region Public Properties

    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }

    #endregion Public Properties

    #region Public Methods

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    #endregion Public Methods
}

public class CraftRole : CraftRole<KeyType>, ICraftRole
{
    #region Public Constructors

    public CraftRole() { }

    public CraftRole(string? name, string? description = null) : base(name, description) { }

    #endregion Public Constructors
}
