using Craft.Domain.Exceptions.Domain;
using Craft.Domain.Exceptions.Http;
using Craft.Domain.Exceptions.Infrastructure;

namespace Craft.Domain.Tests.Exceptions;

public class ContextualExceptionTests
{
    #region Public Methods

    [Fact]
    public void Constructors_ContextualValues_ProduceUsefulMessages()
    {
        Assert.Equal("Entity \"User\" (42) already exists", new AlreadyExistsException("User", 42).Message);
        Assert.Equal("Entity \"User\" (42) was not found.", new NotFoundException("User", 42).Message);
        Assert.Equal("Conflict with resource \"User\": Duplicate", new ConflictException("User", "Duplicate").Message);
        Assert.Contains("\"User\" (42)", new ConcurrencyException("User", 42).Message);
        Assert.Contains("Expected version: v1, Actual version: v2", new ConcurrencyException("User", 42, "v1", "v2").Message);
        Assert.Contains("\"User\" (42)", new GoneException("User", 42).Message);
        Assert.Contains("2026-10-07 01:02:03 UTC", new GoneException("User", 42, new DateTime(2026, 10, 7, 1, 2, 3, DateTimeKind.Utc)).Message);
        Assert.Contains("Expected: 'v1', Actual: 'v2'", new PreconditionFailedException("If-Match", "v1", "v2").Message);
        Assert.Equal("Configuration error for key \"Database\": Missing", new ConfigurationException("Database", "Missing").Message);
        Assert.Equal("Database error during Insert: Failed", new DatabaseException("Insert", "Failed").Message);
        Assert.Equal("External service \"API\" error: Down", new ExternalServiceException("API", "Down").Message);
        Assert.Equal("External service \"API\" returned status 503: Down", new ExternalServiceException("API", 503, "Down").Message);
        Assert.Equal("Bad gateway from \"API\": Invalid", new BadGatewayException("API", "Invalid").Message);
        Assert.Equal("Gateway timeout from \"API\" after 30 seconds", new GatewayTimeoutException("API", 30).Message);
        Assert.Equal("Feature \"Export\" is not implemented: Pending", new FeatureNotImplementedException("Export", "Pending").Message);
        Assert.Equal("Rate limit exceeded: 100 requests per hour", new TooManyRequestsException(100, "hour").Message);
        Assert.Equal("Media type \"text/plain\" is not supported. Supported types: application/json, text/csv",
            new UnsupportedMediaTypeException("text/plain", ["application/json", "text/csv"]).Message);
        Assert.Contains("bytes exceeds maximum", new PayloadTooLargeException(2000, 1000).Message);
        Assert.StartsWith("Image size", new PayloadTooLargeException("Image", 2000, 1000).Message);
    }

    [Fact]
    public void Constructors_InvalidContext_RejectInput()
    {
        Assert.Throws<ArgumentNullException>(() => new NotFoundException("User", key: null!));
        Assert.Throws<ArgumentException>(() => new AlreadyExistsException(" ", 1));
        Assert.Throws<ArgumentException>(() => new ConcurrencyException(" ", 1));
        Assert.Throws<ArgumentException>(() => new ConcurrencyException(" ", 1, "v1", "v2"));
        Assert.Throws<ArgumentException>(() => new GoneException(" ", 1));
        Assert.Throws<ArgumentException>(() => new GoneException(" ", 1, DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() => new ConflictException(" ", "Conflict"));
        Assert.Throws<ArgumentException>(() => new PreconditionFailedException(" ", "v1", "v2"));
        Assert.Throws<ArgumentException>(() => new ConfigurationException(" ", "Missing"));
        Assert.Throws<ArgumentException>(() => new DatabaseException(" ", "Failed"));
        Assert.Throws<ArgumentException>(() => new ExternalServiceException(" ", "Down"));
        Assert.Throws<ArgumentException>(() => new ExternalServiceException(" ", 503, "Down"));
        Assert.Throws<ArgumentException>(() => new FeatureNotImplementedException(" ", "Pending"));
        Assert.Throws<ArgumentException>(() => new PayloadTooLargeException(" ", 2, 1));
        Assert.Throws<ArgumentException>(() => new BadGatewayException(" ", "Invalid"));
        Assert.Throws<ArgumentException>(() => new UnsupportedMediaTypeException(" ", ["application/json"]));
        Assert.Throws<ArgumentException>(() => new GatewayTimeoutException(" ", 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GatewayTimeoutException("API", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TooManyRequestsException(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TooManyRequestsException(0, "hour"));
        Assert.Throws<ArgumentException>(() => new TooManyRequestsException(1, " "));
        Assert.Throws<ArgumentNullException>(() => new UnsupportedMediaTypeException("text/plain", supportedTypes: null!));
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    public void Gone_NonUtcDeletionTime_RejectsInput(DateTimeKind kind)
        => Assert.Throws<ArgumentException>(() => new GoneException("User", 42, new DateTime(2026, 10, 7, 0, 0, 0, kind)));

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(1, -1)]
    public void PayloadTooLarge_NegativeSizes_RejectsInput(long actual, long maximum)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PayloadTooLargeException(actual, maximum));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PayloadTooLargeException("Image", actual, maximum));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(0, 1)]
    public void PayloadTooLarge_WithinLimit_RejectsContradictoryInput(long actual, long maximum)
    {
        Assert.Throws<ArgumentException>(() => new PayloadTooLargeException(actual, maximum));
        Assert.Throws<ArgumentException>(() => new PayloadTooLargeException("Image", actual, maximum));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(60)]
    public void TooManyRequests_RetryDelay_ExposesMetadata(int seconds)
    {
        TooManyRequestsException exception = new(seconds);
        Assert.Equal(TimeSpan.FromSeconds(seconds), exception.RetryAfter);
        Assert.Equal($"Too many requests. Retry after {seconds} seconds", exception.Message);
        Assert.Null(new TooManyRequestsException().RetryAfter);
    }

    #endregion Public Methods
}
