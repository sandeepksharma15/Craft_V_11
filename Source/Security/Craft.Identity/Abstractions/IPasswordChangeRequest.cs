namespace Craft.Identity.Abstractions;

public interface IPasswordChangeRequest<TKey>
{
    #region Public Properties

    string? ConfirmNewPassword { get; set; }
    TKey Id { get; set; }
    string? NewPassword { get; set; }
    string? Password { get; set; }
    string? Token { get; set; }

    #endregion Public Properties
}

public interface IPasswordChangeRequest : IPasswordChangeRequest<KeyType>;
