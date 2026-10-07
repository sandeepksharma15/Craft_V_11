using Craft.Domain.Abstractions;
using Craft.Domain.Base;

namespace Craft.Domain.Tests.Abstractions;

public class AggregateRootBaseTests
{
    #region Private Classes

    private sealed class DefaultRoot(KeyType id) : AggregateRoot(id) { }

    private sealed class GuidRoot(Guid id) : AggregateRoot<Guid>(id) { }

    private sealed class PlainEntity : BaseEntity { }

    #endregion Private Classes

    #region Public Methods

    [Fact]
    public void DefaultBase_ImplementsConfiguredAggregateContract()
    {
        DefaultRoot root = new(42);

        IAggregateRoot contract = root;
        Assert.Equal((KeyType)42, contract.Id);
        Assert.IsType<IAggregateRoot<KeyType>>(root, exactMatch: false);
        Assert.IsType<IModel>(root, exactMatch: false);
        Assert.False((object)root is IHasDomainEvents);
    }

    [Fact]
    public void EntityBase_DoesNotRequireAggregateContract()
    {
        IEntity entity = new PlainEntity();

        Assert.False(entity is IAggregateRoot);
    }

    [Fact]
    public void GenericBase_ImplementsChosenKeyContract()
    {
        Guid id = Guid.NewGuid();
        GuidRoot root = new(id);

        IAggregateRoot<Guid> contract = root;
        Assert.Equal(id, contract.Id);
    }

    #endregion Public Methods
}
