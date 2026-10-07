using Craft.Domain.Exceptions;
using Craft.Domain.Exceptions.Domain;
using Craft.Domain.Exceptions.Http;
using Craft.Domain.Exceptions.Security;

namespace Craft.Domain.Tests.Exceptions;

public class CraftExceptionFactoryTests
{
    #region Public Methods

    [Fact]
    public void FromException_Cancellation_PreservesIdentityAndToken()
    {
        using CancellationTokenSource cancellation = new();
        OperationCanceledException source = new(cancellation.Token);
        Assert.Same(source, CraftExceptionFactory.FromException(source));
        TaskCanceledException taskCancellation = new();
        Assert.Same(taskCancellation, CraftExceptionFactory.FromException(taskCancellation));
    }

    [Fact]
    public void FromException_CraftException_PreservesIdentityAndDetails()
    {
        NotFoundException source = new("Missing", errors: ["Detail"]);
        Assert.Same(source, CraftExceptionFactory.FromException(source));
    }

    [Fact]
    public void FromException_Null_RejectsInput()
        => Assert.Throws<ArgumentNullException>(() => CraftExceptionFactory.FromException(null!));

    [Theory]
    [InlineData(0, typeof(ForbiddenException))]
    [InlineData(1, typeof(GatewayTimeoutException))]
    [InlineData(2, typeof(FeatureNotImplementedException))]
    [InlineData(3, typeof(InternalServerException))]
    [InlineData(4, typeof(InternalServerException))]
    [InlineData(5, typeof(InternalServerException))]
    [InlineData(6, typeof(InternalServerException))]
    [InlineData(7, typeof(InternalServerException))]
    public void FromException_RuntimeFailure_PreservesCauseWithoutInferringClientFault(int sourceType, Type expectedType)
    {
        Exception source = sourceType switch
        {
            0 => new UnauthorizedAccessException("Denied"),
            1 => new TimeoutException("Timeout"),
            2 => new NotImplementedException("Pending"),
            3 => new ArgumentNullException(message: "Null Argument", null),
            4 => new ArgumentException("Argument"),
            5 => new InvalidOperationException("Operation"),
            6 => new KeyNotFoundException("Key"),
            _ => new Exception("Unknown")
        };

        Exception converted = CraftExceptionFactory.FromException(source);
        Assert.IsType(expectedType, converted);
        Assert.Same(source, converted.InnerException);
    }

    [Theory]
    [InlineData(400, typeof(BadRequestException))]
    [InlineData(401, typeof(UnauthorizedException))]
    [InlineData(403, typeof(ForbiddenException))]
    [InlineData(404, typeof(NotFoundException))]
    [InlineData(409, typeof(ConflictException))]
    [InlineData(410, typeof(GoneException))]
    [InlineData(412, typeof(PreconditionFailedException))]
    [InlineData(413, typeof(PayloadTooLargeException))]
    [InlineData(415, typeof(UnsupportedMediaTypeException))]
    [InlineData(422, typeof(UnprocessableEntityException))]
    [InlineData(429, typeof(TooManyRequestsException))]
    [InlineData(500, typeof(InternalServerException))]
    [InlineData(501, typeof(FeatureNotImplementedException))]
    [InlineData(502, typeof(BadGatewayException))]
    [InlineData(503, typeof(ServiceUnavailableException))]
    [InlineData(504, typeof(GatewayTimeoutException))]
    [InlineData(418, typeof(HttpStatusException))]
    [InlineData(599, typeof(HttpStatusException))]
    public void FromStatusCode_ErrorStatus_PreservesMessageCauseAndErrors(int status, Type expectedType)
    {
        Exception cause = new("Cause");
        string[] errors = ["Error"];
        CraftException exception = CraftExceptionFactory.FromStatusCode(status, "Message", errors, cause);

        Assert.IsType(expectedType, exception);
        Assert.Equal(status, exception.StatusCodeValue);
        Assert.Equal("Message", exception.Message);
        Assert.Same(cause, exception.InnerException);
        errors[0] = "Changed";
        Assert.Equal(["Error"], exception.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(200)]
    [InlineData(399)]
    [InlineData(600)]
    [InlineData(999)]
    public void FromStatusCode_NonErrorStatus_RejectsInput(int status)
        => Assert.Throws<ArgumentOutOfRangeException>(() => CraftExceptionFactory.FromStatusCode(status, "Error"));

    [Fact]
    public void FromStatusCode_OptionalArguments_DefaultToEmptyErrorsAndNoCause()
    {
        CraftException exception = CraftExceptionFactory.FromStatusCode(404, "Missing");
        Assert.Empty(exception.Errors);
        Assert.Null(exception.InnerException);
    }

    #endregion Public Methods
}
