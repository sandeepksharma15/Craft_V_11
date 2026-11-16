using System.ComponentModel.DataAnnotations.Schema;
using Craft.Domain;
using Microsoft.AspNetCore.Identity;

namespace Craft.Identity;

public class CraftUser<TKey> : IdentityUser<TKey>, ICraftUser<TKey>
    where TKey : IEquatable<TKey>
{
    public const string UserId = "UserId";

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

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}

public class CraftUser : CraftUser<KeyType>, ICraftUser;

