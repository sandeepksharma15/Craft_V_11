using Craft.Testing.Repositories;

namespace Craft.Repositories.Tests;

public class StubTests
{
    [Fact]
    public void UnconfiguredOperations_FailExplicitly()
    {
        var stub = new EmptyStub(); var row = new Product();
        Func<Task>[] operations = [
            () => stub.AddAsync(row),
            () => stub.AddRangeAsync([row]),
            () => stub.UpdateAsync(row),
            () => stub.UpdateRangeAsync([row]),
            () => stub.DeleteAsync(row),
            () => stub.DeleteRangeAsync([row]),
            () => stub.RestoreAsync(row),
            () => stub.RestoreRangeAsync([row]),
            () => stub.GetAsync(1),
            () => stub.GetAllAsync(),
            () => stub.GetPagedListAsync(1, 10),
            () => stub.GetCountAsync(),
            () => stub.ExistsAsync(1),
            () => stub.AnyAsync(),
            () => stub.LoadForUpdateAsync(1),
            () => stub.LoadDeletedAsync(1),
            () => stub.PermanentlyDeleteAsync(row),
            () => stub.SaveChangesAsync()
        ];
        foreach (var operation in operations) Assert.Throws<NotSupportedException>(() => { _ = operation(); });
        Assert.IsType<IRepository<Note>>(new DefaultStub(), exactMatch: false);
    }

    private sealed class EmptyStub : RepositoryStub<Product, int>;

    private sealed class DefaultStub : RepositoryStub<Note>;
}
