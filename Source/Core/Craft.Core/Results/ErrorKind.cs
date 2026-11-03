namespace Craft.Core;

public enum ErrorKind
{
    Failure,
    Validation,
    NotFound,
    Unauthorized,
    Forbidden,
    Conflict,
    Unavailable,
    Timeout
}