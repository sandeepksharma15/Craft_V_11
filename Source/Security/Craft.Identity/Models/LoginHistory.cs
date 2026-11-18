using System.ComponentModel.DataAnnotations.Schema;
using Craft.Identity.Abstractions;

namespace Craft.Identity.Models;

public class LoginHistory<TKey> : ILoginHistory<TKey> where TKey : IEquatable<TKey>
{
    #region Public Constructors

    public LoginHistory() { }

    public LoginHistory(string lastIpAddress, DateTime? lastLoginOn, TKey userId)
    {
        LastIpAddress = lastIpAddress;
        LastLoginOn = lastLoginOn;
        UserId = userId;
    }

    #endregion Public Constructors

    #region Public Properties

    public TKey Id { get; set; } = default!;

    // IsDeleted is not used in this class, but is required as there is a GlobalQueryFilter On
    // CraftUser that requires it.
    public bool IsDeleted { get; set; }

    public string? LastIpAddress { get; set; }

    public DateTime? LastLoginOn { get; set; }

    public string? Provider { get; set; }

    [ForeignKey("UserId")]
    public TKey UserId { get; set; } = default!;

    #endregion Public Properties
}

[Table("ID_LoginHistory")]
public class LoginHistory : ILoginHistory<KeyType>, IEntity, IModel
{
    #region Public Constructors

    public LoginHistory() { }

    public LoginHistory(string lastIpAddress, DateTime? lastLoginOn, KeyType userId)
    {
        LastIpAddress = lastIpAddress;
        LastLoginOn = lastLoginOn;
        UserId = userId;
    }

    #endregion Public Constructors

    #region Public Properties

    public KeyType Id { get; set; } = default!;

    public bool IsDeleted { get; set; }

    public string? LastIpAddress { get; set; }

    public DateTime? LastLoginOn { get; set; }

    public string? Provider { get; set; }

    [ForeignKey("UserId")]
    public KeyType UserId { get; set; } = default!;

    #endregion Public Properties
}
