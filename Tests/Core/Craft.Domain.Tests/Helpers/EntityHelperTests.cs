using Craft.Domain.Abstractions;
using Craft.Domain.Base;
using Craft.Domain.Helpers;

namespace Craft.Domain.Tests.Helpers;

public class EntityHelperTests
{
    [Theory]
    [InlineData(typeof(IEntity), true)]
    [InlineData(typeof(IEntity<int>), true)]
    [InlineData(typeof(IEntity<>), true)]
    [InlineData(typeof(Entity<Guid>), true)]
    [InlineData(typeof(Entity<>), true)]
    [InlineData(typeof(DerivedEntity), true)]
    [InlineData(typeof(EntityValue), true)]
    [InlineData(typeof(IMergedEntity), true)]
    [InlineData(typeof(AmbiguousEntity), true)]
    [InlineData(typeof(BaseDTO<int>), false)]
    [InlineData(typeof(IHasId<int>), false)]
    [InlineData(typeof(IHasTenant<Guid>), false)]
    [InlineData(typeof(List<>), false)]
    [InlineData(typeof(object), false)]
    public void IsEntity_RecognizesEntityContracts(Type type, bool expected)
        => Assert.Equal(expected, type.IsEntity());

    [Theory]
    [InlineData(typeof(IEntity<int>), true)]
    [InlineData(typeof(Entity<int>), true)]
    [InlineData(typeof(DerivedEntity), true)]
    [InlineData(typeof(AmbiguousEntity), true)]
    [InlineData(typeof(Entity<Guid>), false)]
    [InlineData(typeof(Entity<>), false)]
    [InlineData(typeof(object), false)]
    public void IsEntity_WithKeyType_RequiresMatchingContract(Type type, bool expected)
        => Assert.Equal(expected, type.IsEntity<int>());

    [Theory]
    [InlineData(typeof(IHasTenant), true)]
    [InlineData(typeof(IHasTenant<Guid>), true)]
    [InlineData(typeof(IHasTenant<>), true)]
    [InlineData(typeof(Tenant<Guid>), true)]
    [InlineData(typeof(Tenant<>), true)]
    [InlineData(typeof(DerivedTenant), true)]
    [InlineData(typeof(Entity<int>), false)]
    [InlineData(typeof(IHasUser<Guid>), false)]
    [InlineData(typeof(List<>), false)]
    [InlineData(typeof(object), false)]
    public void IsMultiTenant_RecognizesTenantContractsWithoutRequiringEntity(Type type, bool expected)
        => Assert.Equal(expected, type.IsMultiTenant());

    [Theory]
    [InlineData(typeof(IHasTenant<Guid>), true)]
    [InlineData(typeof(Tenant<Guid>), true)]
    [InlineData(typeof(DerivedTenant), true)]
    [InlineData(typeof(Tenant<int>), false)]
    [InlineData(typeof(Tenant<>), false)]
    [InlineData(typeof(object), false)]
    public void IsMultiTenant_WithKeyType_RequiresMatchingContract(Type type, bool expected)
        => Assert.Equal(expected, type.IsMultiTenant<Guid>());

    [Theory]
    [InlineData(typeof(IEntity), typeof(KeyType))]
    [InlineData(typeof(IEntity<int>), typeof(int))]
    [InlineData(typeof(Entity<Guid>), typeof(Guid))]
    [InlineData(typeof(Entity<string>), typeof(string))]
    [InlineData(typeof(Entity<int?>), typeof(int?))]
    [InlineData(typeof(DerivedEntity), typeof(int))]
    [InlineData(typeof(EntityValue), typeof(int))]
    [InlineData(typeof(IMergedEntity), typeof(int))]
    public void GetEntityKeyType_UniqueContract_ReturnsKey(Type type, Type expected)
    {
        Assert.Equal(expected, type.GetEntityKeyType());
        Assert.True(type.TryGetEntityKeyType(out var key));
        Assert.Equal(expected, key);
    }

    [Theory]
    [InlineData(typeof(IEntity<>))]
    [InlineData(typeof(Entity<>))]
    public void GetEntityKeyType_OpenGeneric_ReturnsGenericParameter(Type type)
        => Assert.Equal(type.GetGenericArguments()[0], type.GetEntityKeyType());

    [Theory]
    [InlineData(typeof(object))]
    [InlineData(typeof(IHasId<int>))]
    [InlineData(typeof(BaseDTO<int>))]
    [InlineData(typeof(AmbiguousEntity))]
    public void GetEntityKeyType_MissingOrAmbiguousContract_FailsExplicitly(Type type)
    {
        Assert.False(type.TryGetEntityKeyType(out var key));
        Assert.Null(key);
        Assert.Throws<ArgumentException>("type", () => type.GetEntityKeyType());
    }

    [Fact]
    public void TypeInspection_NullArgument_ThrowsConsistently()
    {
        Type type = null!;

        Assert.Throws<ArgumentNullException>("type", () => type.IsEntity());
        Assert.Throws<ArgumentNullException>("type", () => type.IsEntity<int>());
        Assert.Throws<ArgumentNullException>("type", () => type.IsMultiTenant());
        Assert.Throws<ArgumentNullException>("type", () => type.IsMultiTenant<Guid>());
        Assert.Throws<ArgumentNullException>("type", () => type.GetEntityKeyType());
        Assert.Throws<ArgumentNullException>("type", () => type.TryGetEntityKeyType(out _));
    }

    private class Entity<TKey> : IEntity<TKey>
    {
        public TKey Id { get; set; } = default!;
    }

    private sealed class DerivedEntity : Entity<int>;

    private struct EntityValue : IEntity<int>
    {
        public int Id { get; set; }
    }

    private interface IFirstEntity : IEntity<int>;
    private interface ISecondEntity : IEntity<int>;
    private interface IMergedEntity : IFirstEntity, ISecondEntity;

    private sealed class AmbiguousEntity : IEntity<int>, IEntity<Guid>
    {
        int IHasId<int>.Id { get; set; }
        Guid IHasId<Guid>.Id { get; set; }
    }

    private class Tenant<TKey> : IHasTenant<TKey>
    {
        public TKey TenantId { get; set; } = default!;
    }

    private sealed class DerivedTenant : Tenant<Guid>;
}