namespace Craft.Identity.Abstractions;

public interface IEmailConfirmationRequest
{
    #region Public Properties

    string? Email { get; set; }
    string? Token { get; set; }

    #endregion Public Properties
}
