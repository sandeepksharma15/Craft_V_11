namespace Craft.Core.Results;

public sealed class Result : IResult
{
    #region Private Constructors

    private Result(IReadOnlyList<Error> errors) => Errors = errors;

    #endregion Private Constructors

    #region Internal Methods

    internal static IReadOnlyList<Error> SnapshotErrors(IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var snapshot = errors.ToArray();

        if (snapshot.Length == 0 || snapshot.Any(error => error is null))
            throw new ArgumentException("A failure requires at least one non-null error.", nameof(errors));

        return Array.AsReadOnly(snapshot);
    }

    #endregion Internal Methods

    #region Public Properties

    public IReadOnlyList<Error> Errors { get; }
    public bool IsFailure => !IsSuccess;
    public bool IsSuccess => Errors.Count == 0;

    #endregion Public Properties

    #region Public Methods

    public static Result Failure(Error error) => Failure([error]);

    public static Result Failure(IEnumerable<Error> errors) => new(SnapshotErrors(errors));

    public static Result Success() => new([]);

    #endregion Public Methods
}
