using System.Diagnostics.CodeAnalysis;

namespace Craft.Core.Results;

public sealed class Result<T> : IResult
{
    #region Private Fields

    private readonly T _value;

    #endregion Private Fields

    #region Private Constructors

    private Result(T value, IReadOnlyList<Error> errors)
    {
        _value = value;
        Errors = errors;
    }

    #endregion Private Constructors

    #region Public Properties

    public IReadOnlyList<Error> Errors { get; }

    public bool IsFailure => !IsSuccess;

    public bool IsSuccess => Errors.Count == 0;

    public T Value => IsSuccess ? _value : throw new InvalidOperationException("A failed result has no value.");

    #endregion Public Properties

    #region Public Methods

    public static Result<T> Failure(Error error) => Failure([error]);

    public static Result<T> Failure(IEnumerable<Error> errors)
        => new(default!, Result.SnapshotErrors(errors));

    public static Result<T> Success(T value) => new(value, []);

    public Result<TNext> Bind<TNext>(Func<T, Result<TNext>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess ? bind(_value) ?? throw new InvalidOperationException("The binder returned null.") : Result<TNext>.Failure(Errors);
    }

    public async Task<Result<TNext>> BindAsync<TNext>(Func<T, CancellationToken, Task<Result<TNext>>> bind, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bind);

        cancellationToken.ThrowIfCancellationRequested();

        return IsSuccess
            ? await bind(_value, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("The binder returned null.")
            : Result<TNext>.Failure(Errors);
    }

    [return: MaybeNull]
    public T GetValueOrDefault() => IsSuccess ? _value : default;

    public T GetValueOrDefault(T fallback) => IsSuccess ? _value : fallback;

    public Result<TNext> Map<TNext>(Func<T, TNext> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return IsSuccess ? Result<TNext>.Success(map(_value)) : Result<TNext>.Failure(Errors);
    }

    public async Task<Result<TNext>> MapAsync<TNext>(Func<T, CancellationToken, Task<TNext>> map, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(map);
        cancellationToken.ThrowIfCancellationRequested();

        return IsSuccess
            ? Result<TNext>.Success(await map(_value, cancellationToken).ConfigureAwait(false))
            : Result<TNext>.Failure(Errors);
    }

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<IReadOnlyList<Error>, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(_value) : onFailure(Errors);
    }

    #endregion Public Methods
}
