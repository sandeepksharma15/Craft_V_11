using System.ComponentModel.DataAnnotations.Schema;
using Craft.Identity.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace Craft.Identity.Models;

public class CraftUser<TKey> : IdentityUser<TKey>, ICraftUser<TKey>
    where TKey : IEquatable<TKey>
{
    #region Public Fields

    public const string UserId = "UserId";

    #endregion Public Fields

    #region Public Constructors

    public CraftUser() => SecurityStamp = Guid.NewGuid().ToString();

    public CraftUser(string? userName, string? email, string? firstName, string? lastName, string? dialCode,
        string? phoneNumber) : this()
    {
        UserName = userName;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        DialCode = dialCode;
        PhoneNumber = phoneNumber;
    }

    #endregion Public Constructors

    #region Public Properties

    public string? DialCode { get; set; }

    public string? FirstName { get; set; }

    [NotMapped]
    public string FullName => string.Join(" ", new[] { FirstName, LastName }.Where(name => !string.IsNullOrWhiteSpace(name)));

    public GenderType Gender { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public string? LastName { get; set; }

    public HonorificType Title { get; set; }

    #endregion Public Properties

    #region Public Methods

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    #endregion Public Methods
}

public class CraftUser : CraftUser<KeyType>, ICraftUser;
