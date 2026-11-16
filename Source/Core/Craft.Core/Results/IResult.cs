namespace Craft.Core.Results;

public interface IResult
{
    #region Public Properties

    IReadOnlyList<Error> Errors { get; }
    bool IsFailure { get; }
    bool IsSuccess { get; }

    #endregion Public Properties
}
