namespace Craft.Domain.Exceptions.Security;

/// <summary>Invalid credentials. HTTP 401.</summary>
public class InvalidCredentialsException : UnauthorizedException
{
    public InvalidCredentialsException(string? message = null, Exception? innerException = null, IEnumerable<string>? errors = null)
        : base(message ?? "Invalid credentials", innerException, errors) { }
}