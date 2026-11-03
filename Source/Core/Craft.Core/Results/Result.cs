namespace Craft.Core;

public sealed class Result : IResult
{
    public bool IsSuccess => Errors.Count == 0;
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<Error> Errors { get; }

    private Result(IReadOnlyList<Error> errors) => Errors = errors;

    public static Result Success() => new(Array.Empty<Error>());
    public static Result Failure(Error error) => Failure([error]);
    public static Result Failure(IEnumerable<Error> errors) => new(SnapshotErrors(errors));

    internal static IReadOnlyList<Error> SnapshotErrors(IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var snapshot = errors.ToArray();
        if (snapshot.Length == 0 || snapshot.Any(error => error is null))
            throw new ArgumentException("A failure requires at least one non-null error.", nameof(errors));
        return Array.AsReadOnly(snapshot);
    }
}