using Craft.Domain.Exceptions;

namespace Craft.Domain.Tests.Exceptions;

public class ExceptionInfoTests
{
    [Fact]
    public void ToErrorInfo_ClientFailure_ExposesMessageErrorsAndStatusWithoutDiagnostics()
    {
        BadRequestException exception = new("Invalid input", new Exception("Private cause"), ["Required"]);
        ExceptionInfo info = exception.ToErrorInfo();

        Assert.Equal("Invalid input", info.Message);
        Assert.Equal(400, info.StatusCode);
        Assert.Equal(["Required"], info.Errors);
        Assert.Null(info.ExceptionType);
        Assert.Null(info.StackTrace);
        Assert.Null(info.InnerException);
        Assert.Null(info.ValidationErrors);
    }

    [Fact]
    public void ToErrorInfo_ServerFailure_HidesMessagesErrorsAndCauses()
    {
        DatabaseException exception = new("Connection password=secret", new Exception("Secret"), ["SQL details"]);
        ExceptionInfo info = exception.ToErrorInfo();

        Assert.Equal("An unexpected error occurred.", info.Message);
        Assert.Equal(500, info.StatusCode);
        Assert.Null(info.Errors);
        Assert.Null(info.ExceptionType);
        Assert.Null(info.StackTrace);
        Assert.Null(info.InnerException);
        Assert.DoesNotContain("secret", JsonSerializer.Serialize(info));
    }

    [Fact]
    public void ToErrorInfo_ValidationFailure_RoundTripsPropertyErrorsThroughJson()
    {
        ModelValidationException exception = new(new Dictionary<string, string[]> { ["Name"] = ["Required"] }, "Validation");
        ExceptionInfo info = exception.ToErrorInfo();
        ExceptionInfo? restored = JsonSerializer.Deserialize<ExceptionInfo>(JsonSerializer.Serialize(info));

        Assert.NotNull(restored);
        Assert.Equal(info.Message, restored.Message);
        Assert.Equal(info.StatusCode, restored.StatusCode);
        Assert.Equal(["Required"], restored.Errors);
        Assert.Equal(["Required"], restored.ValidationErrors!["Name"]);
    }

    [Fact]
    public void ToErrorInfo_NoErrors_OmitsErrors()
        => Assert.Null(new NotFoundException().ToErrorInfo().Errors);

    [Fact]
    public void ToErrorInfo_Diagnostics_CapturesMixedExceptionChainAndStackTraces()
    {
        NotFoundException root = Assert.Throws<NotFoundException>((Action)(() => { throw new NotFoundException("Root", errors: ["Missing"]); }));
        InvalidOperationException middle = Assert.Throws<InvalidOperationException>((Action)(() => { throw new InvalidOperationException("Middle", root); }));
        DatabaseException outer = Assert.Throws<DatabaseException>((Action)(() => { throw new DatabaseException("Database details", middle, ["Query failed"]); }));
        ExceptionInfo info = outer.ToErrorInfo(includeDetails: true);

        Assert.Equal(nameof(DatabaseException), info.ExceptionType);
        Assert.Equal("Database details", info.Message);
        Assert.Equal(["Query failed"], info.Errors);
        Assert.NotNull(info.StackTrace);
        Assert.NotNull(info.InnerException);
        Assert.Equal(nameof(InvalidOperationException), info.InnerException.ExceptionType);
        Assert.Equal("Middle", info.InnerException.Message);
        Assert.NotNull(info.InnerException.StackTrace);
        Assert.Null(info.InnerException.StatusCode);
        Assert.Null(info.InnerException.Errors);
        ExceptionInfo? rootInfo = info.InnerException.InnerException;
        Assert.NotNull(rootInfo);
        Assert.Equal(404, rootInfo.StatusCode);
        Assert.Equal(nameof(NotFoundException), rootInfo.ExceptionType);
        Assert.Equal(["Missing"], rootInfo.Errors);
        Assert.Null(rootInfo.InnerException);
        Assert.NotNull(rootInfo.StackTrace);

        ExceptionInfo? restored = JsonSerializer.Deserialize<ExceptionInfo>(JsonSerializer.Serialize(info));
        Assert.Equal(info.ExceptionType, restored!.ExceptionType);
        Assert.Equal(info.StackTrace, restored.StackTrace);
        Assert.Equal(rootInfo.Message, restored.InnerException!.InnerException!.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(100)]
    public void ToErrorInfo_DeepChain_IsBounded(int depth)
    {
        Exception cause = new("Root");
        for (int index = 1; index < depth; index++)
            cause = new Exception("Inner", cause);
        DatabaseException exception = new(innerException: cause);
        ExceptionInfo? current = exception.ToErrorInfo(includeDetails: true);
        int count = 0;
        while (current is not null)
        {
            count++;
            current = current.InnerException;
        }
        Assert.Equal(Math.Min(depth + 1, 16), count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToErrorInfo_RateLimit_RoundTripsRetryMetadata(bool includeDetails)
    {
        ExceptionInfo info = new TooManyRequestsException(60).ToErrorInfo(includeDetails);
        ExceptionInfo? restored = JsonSerializer.Deserialize<ExceptionInfo>(JsonSerializer.Serialize(info));
        Assert.Equal(TimeSpan.FromSeconds(60), restored!.RetryAfter);
        Assert.Equal(429, restored.StatusCode);
    }

    [Fact]
    public void ToErrorInfo_ValidationDiagnostics_PreservesPropertyErrors()
    {
        ModelValidationException exception = new(new Dictionary<string, string[]> { ["Name"] = ["Required"] }, "Validation");
        ExceptionInfo info = exception.ToErrorInfo(includeDetails: true);
        Assert.Equal(["Required"], info.ValidationErrors!["Name"]);
    }

    [Fact]
    public void ExceptionInfo_EmptyInstance_RoundTripsDefaultProperties()
    {
        ExceptionInfo info = new();
        Assert.Equal(info, JsonSerializer.Deserialize<ExceptionInfo>(JsonSerializer.Serialize(info)));
    }
}