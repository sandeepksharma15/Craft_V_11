namespace Craft.Identity.Abstractions;

public interface IPasswordResetRequest<TKey>
{
    #region Public Properties

    string? Email { get; set; }
    TKey Id { get; set; }
    string? Password { get; set; }
    string? Token { get; set; }

    #endregion Public Properties
}

public interface IPasswordResetRequest : IPasswordResetRequest<KeyType>;
