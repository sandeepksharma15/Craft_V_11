using System.ComponentModel.DataAnnotations;
using Craft.Identity.Abstractions;

namespace Craft.Identity.Models;

public class EmailConfirmationRequest : IEmailConfirmationRequest
{
    #region Public Properties

    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    public string? Token { get; set; }

    #endregion Public Properties
}
