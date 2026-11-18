using System.ComponentModel.DataAnnotations;
using Craft.Identity.Abstractions;

namespace Craft.Identity.Models;

public class PasswordForgotRequest : IPasswordForgotRequest
{
    #region Public Properties

    [Required]
    [Url]
    [DataType(DataType.Url)]
    public string? ClientURI { get; set; }

    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    #endregion Public Properties
}
