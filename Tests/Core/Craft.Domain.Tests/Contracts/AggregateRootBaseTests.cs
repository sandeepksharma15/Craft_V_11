using Craft.Domain.Abstractions;
using Craft.Domain.Base;

namespace Craft.Domain.Tests.Contracts;

public class AggregateRootBaseTests
{
    [Fact]
    public void DefaultBase_ImplementsConfiguredAggregateContract()
    {
        DefaultRoot root = new(42);

        IAggregateRoot contract = root;
        Assert.Equal((KeyType)42, contract.Id);
        Assert.IsAssignableFrom<IAggregateRoot<KeyType>>(root);
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

    private sealed class DefaultRoot(KeyType id) : AggregateRoot(id) { }
    private sealed class GuidRoot(Guid id) : AggregateRoot<Guid>(id) { }
    private sealed class PlainEntity : BaseEntity { }
}
