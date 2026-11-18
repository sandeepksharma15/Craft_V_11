using System.ComponentModel.DataAnnotations;
using Craft.Identity.Abstractions;

namespace Craft.Identity.Models;

public class PasswordChangeRequest<TKey> : IPasswordChangeRequest<TKey>
{
    #region Public Properties

    [Required]
    [DataType(DataType.Password)]
    public string? ConfirmNewPassword { get; set; } = default!;

    public TKey Id { get; set; } = default!;

    [Required]
    [DataType(DataType.Password)]
    public string? NewPassword { get; set; } = default!;

    // Token is used by the reset-password flow only; not required for change-password.
    public string? Token { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string? Password { get; set; } = default!;

    #endregion Public Properties
}

public class PasswordChangeRequest : PasswordChangeRequest<KeyType>, IPasswordChangeRequest;
