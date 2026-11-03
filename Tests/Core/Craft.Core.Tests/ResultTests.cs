namespace Craft.Core.Tests;

public class ResultTests
{
    private static Error[] Errors => [new("name.required", "Name is required.", ErrorKind.Validation, "Name"), new("email.invalid", "Email is invalid.", ErrorKind.Validation, "Email")];

    [Fact]
    public void Success_PreservesNullAndDefaultValues()
    {
        var empty = Result.Success();
        Assert.True(empty.IsSuccess);
        Assert.False(empty.IsFailure);
        Assert.Empty(empty.Errors);
        var nullable = Result<string?>.Success(null);
        Assert.True(nullable.IsSuccess);
        Assert.False(nullable.IsFailure);
        Assert.Null(nullable.Value);
        Assert.Null(nullable.GetValueOrDefault("fallback"));
        Assert.Equal("null", nullable.Map(v => v ?? "null").Value);
        Assert.Equal("bound", nullable.Bind(v => Result<string>.Success(v ?? "bound")).Value);
        Assert.Equal("success", nullable.Match(_ => "success", _ => "failure"));
        Assert.Equal(0, Result<int>.Success(0).Value);
        Assert.Empty(nullable.Errors);
    }

    [Fact]
    public void Failure_SnapshotsAllErrorsAndPreservesThemThroughComposition()
    {
        var errors = Errors;
        var failure = Result<int>.Failure(errors);
        var plain = Result.Failure(errors);
        errors[0] = new Error("changed", "Changed");
        Assert.True(plain.IsFailure);
        Assert.False(plain.IsSuccess);
        Assert.True(failure.IsFailure);
        Assert.Equal("name.required", failure.Errors[0].Code);
        Assert.Equal("name.required", plain.Errors[0].Code);
        Assert.Throws<NotSupportedException>(() => ((IList<Error>)failure.Errors).Clear());
        Assert.Throws<InvalidOperationException>(() => failure.Value);
        Assert.Equal(-1, failure.GetValueOrDefault(-1));
        Assert.Equal(2, failure.Match(_ => throw new Exception(), e => e.Count));
        Assert.Equal(failure.Errors, failure.Map<string>(_ => throw new Exception()).Errors);
        Assert.Equal(failure.Errors, failure.Bind<string>(_ => throw new Exception()).Errors);
        Assert.Single(Result.Failure(Errors[0]).Errors);
        Assert.Single(Result<int>.Failure(Errors[0]).Errors);
    }

    [Fact]
    public void Errors_AreStructuredImmutableValues()
    {
        var error = Errors[0];
        Assert.Equal("Name is required.", error.Message);
        Assert.Equal(ErrorKind.Validation, error.Kind);
        Assert.Equal("Name", error.Target);
        Assert.Equal(new Error("name.required", "Name is required.", ErrorKind.Validation, "Name"), error);
        var generic = new Error("failed", "Failed");
        Assert.Equal(ErrorKind.Failure, generic.Kind);
        Assert.Null(generic.Target);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Errors_RejectEmptyCodeOrMessage(string? text)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Error(text!, "message"));
        Assert.ThrowsAny<ArgumentException>(() => new Error("code", text!));
    }

    [Fact]
    public void FailureAndComposition_RejectInvalidArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Error("code", "message", (ErrorKind)999));
        Assert.Throws<ArgumentException>(() => new Error("code", "message", target: " "));
        Assert.Throws<ArgumentNullException>(() => Result.Failure((IEnumerable<Error>)null!));
        Assert.Throws<ArgumentException>(() => Result.Failure(Array.Empty<Error>()));
        Assert.Throws<ArgumentException>(() => Result.Failure((Error)null!));
        Assert.Throws<ArgumentException>(() => Result<int>.Failure([Errors[0], null!]));
        var result = Result<int>.Success(1);
        Assert.Throws<ArgumentNullException>(() => result.Map<string>(null!));
        Assert.Throws<ArgumentNullException>(() => result.Bind<string>(null!));
        Assert.Throws<ArgumentNullException>(() => result.Match<string>(null!, _ => "failed"));
        Assert.Throws<ArgumentNullException>(() => result.Match<string>(_ => "ok", null!));
        Assert.Throws<InvalidOperationException>(() => result.Bind<string>(_ => null!));
    }

    [Fact]
    public async Task AsyncComposition_PreservesNullFailuresAndCancellationToken()
    {
        var token = TestContext.Current.CancellationToken;
        var success = Result<string?>.Success(null);
        Assert.Equal("mapped", (await success.MapAsync((value, ct) =>
        {
            Assert.Null(value); Assert.Equal(token, ct);
            return Task.FromResult("mapped");
        }, token)).Value);
        Assert.Equal("bound", (await success.BindAsync((value, ct) =>
        {
            Assert.Null(value); Assert.Equal(token, ct);
            return Task.FromResult(Result<string>.Success("bound"));
        }, token)).Value);
        var failure = Result<int>.Failure(Errors);
        Assert.Equal(failure.Errors, (await failure.MapAsync<string>((_, _) => throw new Exception(), token)).Errors);
        Assert.Equal(failure.Errors, (await failure.BindAsync<string>((_, _) => throw new Exception(), token)).Errors);
        Assert.Equal(Errors, (await success.BindAsync((_, _) => Task.FromResult(Result<string>.Failure(Errors)), token)).Errors);
    }

    [Fact]
    public async Task AsyncComposition_DoesNotConvertExceptionsOrCancellationIntoFailures()
    {
        var result = Result<int>.Success(1);
        await Assert.ThrowsAsync<ArgumentNullException>(() => result.MapAsync<string>(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => result.BindAsync<string>(null!));
        await Assert.ThrowsAsync<InvalidOperationException>(() => result.BindAsync<string>((_, _) => Task.FromResult<Result<string>>(null!)));
        var canceled = new CancellationToken(true);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => result.MapAsync((v, _) => Task.FromResult(v), canceled));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => result.BindAsync((v, _) => Task.FromResult(Result<int>.Success(v)), canceled));
        await Assert.ThrowsAsync<ApplicationException>(() => result.MapAsync<int>((_, _) => Task.FromException<int>(new ApplicationException())));
        await Assert.ThrowsAsync<ApplicationException>(() => result.BindAsync<int>((_, _) => Task.FromException<Result<int>>(new ApplicationException())));
    }
}