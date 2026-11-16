namespace Craft.Core.Results;

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
