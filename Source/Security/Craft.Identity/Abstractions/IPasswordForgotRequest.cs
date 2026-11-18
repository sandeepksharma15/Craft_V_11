namespace Craft.Identity.Abstractions;

public interface IPasswordForgotRequest
{
    #region Public Properties

    string? ClientURI { get; set; }
    string? Email { get; set; }

    #endregion Public Properties
}
