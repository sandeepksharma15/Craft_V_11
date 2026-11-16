namespace Craft.Identity;
public interface ICreateUserRequest
{
    string? ClientURI { get; set; }
    string? Email { get; set; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Password { get; set; }
}
