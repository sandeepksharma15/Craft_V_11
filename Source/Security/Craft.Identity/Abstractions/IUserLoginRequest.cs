namespace Craft.Identity.Abstractions;

public interface IUserLoginRequest
{
    #region Public Properties

    string? Email { get; set; }
    string? IpAddress { get; set; }
    string? Password { get; set; }
    bool RememberMe { get; set; }

    #endregion Public Properties
}
