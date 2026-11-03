namespace Craft.Core;

public sealed record Error
{
    public string Code { get; }
    public string Message { get; }
    public ErrorKind Kind { get; }
    public string? Target { get; }

    public Error(string code, string message, ErrorKind kind = ErrorKind.Failure, string? target = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (target is not null) ArgumentException.ThrowIfNullOrWhiteSpace(target);
        Code = code;
        Message = message;
        Kind = kind;
        Target = target;
    }
}