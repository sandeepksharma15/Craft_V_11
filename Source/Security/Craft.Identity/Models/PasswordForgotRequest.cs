using System.ComponentModel.DataAnnotations;

namespace Craft.Identity;

public class PasswordForgotRequest : IPasswordForgotRequest
{
    [Required]
    [Url]
    [DataType(DataType.Url)]
    public string? ClientURI { get; set; }

    [Required]
    [EmailAddress]
    public string? Email { get; set; }
}

