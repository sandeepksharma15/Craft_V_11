using System.ComponentModel.DataAnnotations;

namespace Craft.Identity;

public class PasswordResetRequest<TKey> : IPasswordResetRequest<TKey>
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    public TKey Id { get; set; } = default!;

    [Required]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Required]
    public string? Token { get; set; }
}

public class ResetPasswordRequest : PasswordResetRequest<KeyType>, IPasswordResetRequest;

