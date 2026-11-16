using System.ComponentModel.DataAnnotations;

namespace Craft.Identity;
public class CreateUserRequest : ICreateUserRequest
{
    [Required]
    [Url]
    public string? ClientURI { get; set; }

    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    public string? FirstName { get; set; }

    [Required]
    public string? LastName { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string? Password { get; set; }
}
