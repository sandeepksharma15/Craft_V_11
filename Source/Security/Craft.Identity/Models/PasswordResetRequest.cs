using System.ComponentModel.DataAnnotations;
using Craft.Identity.Abstractions;

namespace Craft.Identity.Models;

public class PasswordResetRequest<TKey> : IPasswordResetRequest<TKey>
{
    #region Public Properties

    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    public TKey Id { get; set; } = default!;

    [Required]
    public string? Token { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    #endregion Public Properties
}

public class ResetPasswordRequest : PasswordResetRequest<KeyType>, IPasswordResetRequest;
