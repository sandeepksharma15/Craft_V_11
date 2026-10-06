using Craft.Domain.Abstractions;
using Craft.Domain.Base;

namespace Craft.Domain.Tests.Contracts;

public class AggregateRootBaseTests
{
    [Fact]
    public void DefaultBase_ImplementsLongAggregateContract()
    {
        LongRoot root = new(42);

        IAggregateRoot contract = root;
        Assert.Equal(42L, contract.Id);
        Assert.IsAssignableFrom<IAggregateRoot<long>>(root);
        Assert.False((object)root is IHasDomainEvents);
    }

    [Fact]
    public void GenericBase_ImplementsChosenKeyContract()
    {
        Guid id = Guid.NewGuid();
        GuidRoot root = new(id);

        IAggregateRoot<Guid> contract = root;
        Assert.Equal(id, contract.Id);
    }

    [Fact]
    public void EntityBase_DoesNotRequireAggregateContract()
    {
        IEntity entity = new PlainEntity();

        Assert.False(entity is IAggregateRoot);
    }

    private sealed class LongRoot(long id) : AggregateRoot(id) { }
    private sealed class GuidRoot(Guid id) : AggregateRoot<Guid>(id) { }
    private sealed class PlainEntity : BaseEntity { }
}
