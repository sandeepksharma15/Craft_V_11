namespace Craft.Identity;
public interface IEmailConfirmationRequest
{
    string? Email { get; set; }
    string? Token { get; set; }
}
