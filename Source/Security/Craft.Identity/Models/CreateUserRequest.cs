using System.ComponentModel.DataAnnotations;
using Craft.Identity.Abstractions;

namespace Craft.Identity.Models;

public class CreateUserRequest : ICreateUserRequest
{
    #region Public Properties

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

    #endregion Public Properties
}
