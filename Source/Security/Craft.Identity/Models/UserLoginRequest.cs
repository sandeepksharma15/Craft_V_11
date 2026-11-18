using System.ComponentModel.DataAnnotations;
using Craft.Identity.Abstractions;

namespace Craft.Identity.Models;

public class UserLoginRequest : IUserLoginRequest
{
    #region Public Properties

    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    public string? IpAddress { get; set; }

    public bool RememberMe { get; set; } = true;

    [Required]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    #endregion Public Properties
}
