namespace Craft.Identity.Abstractions;

public interface ICreateUserRequest
{
    #region Public Properties

    string? ClientURI { get; set; }
    string? Email { get; set; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Password { get; set; }

    #endregion Public Properties
}
