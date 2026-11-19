using System.Net;
using System.Text;
using Mapster;
using MapsterMapper;

namespace Craft.HttpServices.Tests;

public sealed class ContractTests
{
    private static IMapper CreateMapper() => new Mapper(new TypeAdapterConfig());

    [Fact]
    public void Constructors_SupportDefaultKeysAndValidateDependencies()
    {
        using HttpClient client = new();
        IMapper mapper = CreateMapper();
        Assert.NotNull(new HttpReadService<string>(client, "api/items", mapper));
        Assert.NotNull(new HttpCrudService<string, Input, Input>(client, "api/items", mapper));
        Assert.NotNull(new HttpSoftDeleteService<string, Input, Input>(client, "api/items", mapper));
        Assert.Throws<ArgumentNullException>(() => new HttpReadService<string>(client, "api/items", null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("/api/items")]
    [InlineData("api/")]
    [InlineData("api/./items")]
    [InlineData("api/../items")]
    [InlineData("api?q=1")]
    [InlineData("https://example.test")]
    [InlineData("api/%2f")]
    public void EndpointValidation_PreventsAmbiguousOrExternalRoutes(string endpoint)
    {
        using HttpClient client = new();
        Assert.ThrowsAny<ArgumentException>(() => new HttpReadService<string>(client, endpoint, CreateMapper()));
    }

    [Fact]
    public async Task Keys_AreFormattedInvariantlyAndEscapedAsSingleSegments()
    {
        using HttpClient client = new(new CaptureHandler()) { BaseAddress = new Uri("https://example.test/prefix/") };
        HttpReadService<string, string> service = new(client, "api/my-items_2", CreateMapper());
        HttpResult<string> result = await service.GetAsync("a ?#%", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("https://example.test/prefix/api/my-items_2/items/a%20%3F%23%25?includeDetails=false", result.Outcome.Value);

        HttpReadService<string, decimal> numeric = new(client, "api/items", CreateMapper());
        HttpResult<string> number = await numeric.GetAsync(1.25m, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains("/1.25?", number.Outcome.Value);
    }

    [Fact]
    public async Task Guards_RejectInvalidArgumentsWithoutSending()
    {
        using HttpClient client = new();
        HttpSoftDeleteService<string, Input, Input, string> service = new(client, "api/items", CreateMapper());
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetAsync(null!, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetAsync("", cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetAsync(".", cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetAsync("..", cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetAsync("a/b", cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetAsync("a\\b", cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.GetPageAsync(0, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.GetPageAsync(1, 0, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateAsync("1", null!, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateBatchAsync(null!, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateBatchAsync([], cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateBatchAsync([null!], cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateBatchAsync([], cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteBatchAsync([], cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => service.RestoreBatchAsync([], cancellationToken: TestContext.Current.CancellationToken));
        HttpReadService<string, NullKey> nullKey = new(client, "api/items", CreateMapper());
        await Assert.ThrowsAsync<ArgumentException>(() => nullKey.GetAsync(new NullKey(), cancellationToken: TestContext.Current.CancellationToken));
    }

    public sealed record Input(string Name);

    private sealed class NullKey
    {
        public override string ToString() => null!;
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(request.RequestUri!.AbsoluteUri), Encoding.UTF8, "application/json")
            });
    }
}
