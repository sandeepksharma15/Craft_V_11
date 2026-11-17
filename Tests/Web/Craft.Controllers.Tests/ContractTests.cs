using Craft.Domain.Abstractions;
using Craft.Repositories;
using Craft.Testing.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Craft.Controllers.Tests;

public class ContractTests
{
    [Fact]
    public void Construction_ValidatesDependenciesAndSupportsDefaultKeyAliases()
    {
        var options = Options.Create(new RepositoryOptions()); var repository = new Stub();
        Assert.Throws<ArgumentNullException>(() => new Read(null!, options));
        Assert.Throws<ArgumentNullException>(() => new Read(repository, null!));
        Assert.Throws<ArgumentException>(() => new Read(repository, Options.Create(new RepositoryOptions { MaximumPageSize = 0 })));
        Assert.Throws<ArgumentException>(() => new Read(repository, Options.Create(new RepositoryOptions { MaximumBatchSize = 0 })));
        Assert.NotNull(new Read(repository, options)); Assert.NotNull(new Change(repository, options)); Assert.NotNull(new Soft(repository, options));
        Assert.Throws<ArgumentNullException>(() => ControllerRegistration.AddCraftControllers(null!));
        Assert.Throws<ArgumentNullException>(() => ConcurrencyTags.Get(null!));
        Assert.Throws<InvalidOperationException>(() => ConcurrencyTags.Get(new Item()));
    }

    [Fact]
    public async Task ExceptionHandling_PreservesRequestCancellationAndSanitizesOtherCancellation()
    {
        var handler = new CraftControllerExceptionHandler(NullLogger<CraftControllerExceptionHandler>.Instance);
        var context = new DefaultHttpContext { RequestAborted = new CancellationToken(true) };
        Assert.False(await handler.TryHandleAsync(context, new OperationCanceledException(), TestContext.Current.CancellationToken));
        context.RequestAborted = CancellationToken.None; context.Response.Body = new MemoryStream();
        Assert.True(await handler.TryHandleAsync(context, new OperationCanceledException("secret"), TestContext.Current.CancellationToken));
        Assert.Equal(500, context.Response.StatusCode);
    }

    private sealed class Row : IEntity, ISoftDelete { public KeyType Id { get; set; } public bool IsDeleted { get; set; } }
    private sealed class Stub : RepositoryStub<Row>;
    [NonController]
    private sealed class Read(IReadRepository<Row> repository, IOptions<RepositoryOptions> options) : EntityReadController<Row, string>(repository, options)
    { protected override string MapResponse(Row entity) => "row"; }
    [NonController]
    private sealed class Change(IRepository<Row> repository, IOptions<RepositoryOptions> options) : EntityController<Row, string, Input, Input>(repository, options)
    {
        protected override string MapResponse(Row entity) => "row";
        protected override Row CreateEntity(Input model) => new();
        protected override void ApplyUpdate(Input model, Row entity) { }
    }
    [NonController]
    private sealed class Soft(IRepository<Row> repository, IOptions<RepositoryOptions> options) : SoftDeleteEntityController<Row, string, Input, Input>(repository, options)
    {
        protected override string MapResponse(Row entity) => "row";
        protected override Row CreateEntity(Input model) => new();
        protected override void ApplyUpdate(Input model, Row entity) { }
    }
}
