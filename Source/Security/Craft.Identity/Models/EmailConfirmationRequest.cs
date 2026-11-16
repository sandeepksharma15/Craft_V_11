using System.ComponentModel.DataAnnotations;

namespace Craft.Identity;
public class EmailConfirmationRequest : IEmailConfirmationRequest
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    public string? Token { get; set; }
}
