using System.Net;
using Craft.Domain.Exceptions;

namespace Craft.Domain.Tests.Exceptions;

public class ExceptionContractTests
{
    public static TheoryData<Type, int> ExceptionTypes => new()
    {
        { typeof(AlreadyExistsException), 409 },
        { typeof(ConcurrencyException), 409 },
        { typeof(ConflictException), 409 },
        { typeof(NotFoundException), 404 },
        { typeof(ModelValidationException), 400 },
        { typeof(BadRequestException), 400 },
        { typeof(GoneException), 410 },
        { typeof(PreconditionFailedException), 412 },
        { typeof(PayloadTooLargeException), 413 },
        { typeof(TooManyRequestsException), 429 },
        { typeof(UnsupportedMediaTypeException), 415 },
        { typeof(FeatureNotImplementedException), 501 },
        { typeof(BadGatewayException), 502 },
        { typeof(GatewayTimeoutException), 504 },
        { typeof(InternalServerException), 500 },
        { typeof(ServiceUnavailableException), 503 },
        { typeof(ForbiddenException), 403 },
        { typeof(UnauthorizedException), 401 },
        { typeof(InvalidCredentialsException), 401 },
        { typeof(ConfigurationException), 500 },
        { typeof(DatabaseException), 500 },
        { typeof(ExternalServiceException), 502 },
        { typeof(UnprocessableEntityException), 422 }
    };

    [Theory]
    [MemberData(nameof(ExceptionTypes))]
    public void Constructor_AllExceptionTypes_HaveConsistentImmutableContracts(Type type, int statusCode)
    {
        System.Reflection.ConstructorInfo? constructor = type.GetConstructor(
            [typeof(string), typeof(Exception), typeof(IEnumerable<string>)]);
        Assert.NotNull(constructor);

        CraftException defaults = Assert.IsAssignableFrom<CraftException>(constructor.Invoke([null, null, null]));
        Assert.False(string.IsNullOrWhiteSpace(defaults.Message));
        Assert.Equal(statusCode, defaults.StatusCodeValue);
        Assert.Equal((HttpStatusCode)statusCode, defaults.StatusCode);
        Assert.Empty(defaults.Errors);
        Assert.Null(defaults.InnerException);

        List<string> errors = ["First", "Second"];
        Exception cause = new("Cause");
        CraftException custom = Assert.IsAssignableFrom<CraftException>(constructor.Invoke(["Custom", cause, errors]));
        errors.Clear();

        Assert.Equal("Custom", custom.Message);
        Assert.Same(cause, custom.InnerException);
        Assert.Equal(statusCode, custom.StatusCodeValue);
        Assert.Equal(["First", "Second"], custom.Errors);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)custom.Errors).Add("Changed"));
    }

    [Fact]
    public void ExceptionTypes_AreAllIncludedInContractTests()
    {
        Type[] actual = typeof(CraftException).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(CraftException)) && !type.IsAbstract && type != typeof(HttpStatusException))
            .OrderBy(type => type.Name).ToArray();
        Type[] expected = ExceptionTypes.Select(row => row.Data.Item1).OrderBy(type => type.Name).ToArray();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_EmptyMessage_RejectsInput(string message)
        => Assert.Throws<ArgumentException>(() => new BadRequestException(message));

    [Theory]
    [InlineData(399)]
    [InlineData(600)]
    public void HttpStatusException_NonErrorStatus_RejectsInput(int status)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new HttpStatusException((HttpStatusCode)status, "Error"));

    [Fact]
    public void Errors_LazySource_IsEnumeratedOnceAndSnapshotted()
    {
        int enumerations = 0;
        IEnumerable<string> Source()
        {
            enumerations++;
            yield return "Error";
        }

        BadRequestException exception = new(errors: Source());
        Assert.Equal(["Error"], exception.Errors);
        Assert.Equal(["Error"], exception.Errors);
        Assert.Equal(1, enumerations);
    }

    [Fact]
    public void SpecializedExceptions_AreCatchableByTheirGeneralCategory()
    {
        Assert.IsAssignableFrom<ConflictException>(new AlreadyExistsException());
        Assert.IsAssignableFrom<ConflictException>(new ConcurrencyException());
        Assert.IsAssignableFrom<UnauthorizedException>(new InvalidCredentialsException());
        Assert.IsAssignableFrom<BadGatewayException>(new ExternalServiceException());
    }
}