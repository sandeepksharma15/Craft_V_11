namespace Craft.Core;

public interface IResult
{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    IReadOnlyList<Error> Errors { get; }
}