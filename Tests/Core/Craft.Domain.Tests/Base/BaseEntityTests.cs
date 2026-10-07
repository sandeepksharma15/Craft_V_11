using Craft.Domain.Abstractions;
using Craft.Domain.Base;

namespace Craft.Domain.Tests.Base;

public class BaseEntityTests
{
    #region Private Classes

    private sealed class GuidEntity : BaseEntity<Guid> { }

    private class MockEntity : BaseEntity
    {
        #region Public Constructors

        public MockEntity()
        { }

        public MockEntity(KeyType id) : base(id)
        {
        }

        #endregion Public Constructors
    }

    private class MockTenantEntity(KeyType id, KeyType tenantId) : BaseEntity(id), IHasTenant
    {
        #region Protected Methods

        protected override bool AdditionalEqualityCheck(BaseEntity<KeyType> other)
            => other is IHasTenant otherTenant
               && EqualityComparer<KeyType>.Default.Equals(TenantId, otherTenant.TenantId);

        #endregion Protected Methods

        #region Public Properties

        public KeyType TenantId { get; set; } = tenantId;

        #endregion Public Properties
    }

    private sealed class NullableEntity : BaseEntity<long?> { }

    private sealed class StringEntity : BaseEntity<string?> { }

    #endregion Private Classes

    #region Public Methods

    [Fact]
    public void ConcurrencyStamp_Should_Be_NewGuid()
    {
        // Arrange
        var entity = new MockEntity(1);

        // Act Assert
        Assert.NotNull(entity.ConcurrencyStamp);
        Assert.True(Guid.TryParse(entity.ConcurrencyStamp, out _));
    }

    [Fact]
    public void EqualityOperator_Should_Return_False_For_Different_Id()
    {
        // Arrange
        var entity1 = new MockEntity(1);
        var entity2 = new MockEntity(2);

        // Act
        bool result = entity1 == entity2;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void EqualityOperator_Should_Return_True_For_Same_Id()
    {
        // Arrange
        var entity1 = new MockEntity(1);
        var entity2 = new MockEntity(1);

        // Act
        bool result = entity1 == entity2;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void EqualityOperators_NullOperands_AreConsistent()
    {
        BaseEntity<KeyType>? missing = null;
        MockEntity entity = new(1);

        Assert.Null(missing);
        Assert.Null(missing);
        Assert.False(entity == missing);
        Assert.False(missing == entity);
        Assert.True(entity != missing);
        Assert.True(missing != entity);
    }

    [Fact]
    public void Equals_ConcurrencyAndDeletionState_DoNotChangeIdentity()
    {
        MockEntity first = new(42) { ConcurrencyStamp = "first" };
        MockEntity second = new(42) { ConcurrencyStamp = "second", IsDeleted = true };

        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equals_DefaultIdSameReference_ReturnsTrue()
    {
        MockEntity entity = new();
        BaseEntity<KeyType> same = entity;

        Assert.True(entity.Equals(same));
        Assert.True(entity == same);
        Assert.False(entity != same);
    }

    [Fact]
    public void Equals_DistinctDefaultIds_ReturnsFalse()
    {
        MockEntity first = new();
        MockEntity second = new();

        Assert.False(first.Equals(second));
        Assert.False(first == second);
        Assert.True(first != second);
        Assert.Equal(2, new HashSet<MockEntity> { first, second }.Count);
    }

    [Fact]
    public void Equals_GuidKeys_RequireAssignedIdentity()
    {
        GuidEntity first = new();
        GuidEntity second = new();
        Assert.False(first.Equals(second));

        Guid id = Guid.NewGuid();
        first.Id = id;
        second.Id = id;
        Assert.True(first.Equals(second));
        Assert.False(first.IsNew());
        Assert.Equal(first.GetHashCode(), second.GetHashCode());

        second.Id = Guid.NewGuid();
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Equals_NullableKeys_TreatNullAsDefaultAndZeroAsAssigned()
    {
        NullableEntity first = new();
        NullableEntity second = new();
        Assert.False(first.Equals(second));
        Assert.True(first.IsNew());

        first.Id = 0;
        second.Id = 0;
        Assert.True(first.Equals(second));
        Assert.False(first.IsNew());
    }

    [Fact]
    public void Equals_ReferenceKeys_HandleNullAndAssignedIds()
    {
        StringEntity first = new();
        StringEntity second = new();
        Assert.True(first.IsNew());
        Assert.False(first.Equals(second));
        Assert.True(first.Equals(first));

        first.Id = "device-1";
        second.Id = "device-1";
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.False(first.IsNew());
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentEntities()
    {
        // Arrange
        var entity1 = new MockEntity(1);
        var entity2 = new MockEntity(2);

        // Act
        var result = entity1.Equals(entity2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentTenants()
    {
        // Arrange
        var entity1 = new MockTenantEntity(1, 1001);
        var entity2 = new MockTenantEntity(1, 1002);

        // Act
        var result = entity1.Equals(entity2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentTypes()
    {
        // Arrange
        var entity = new MockEntity(1);
        var otherObject = new object();

        // Act
        var result = entity.Equals(otherObject);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentTypes_DerivedFromEntity()
    {
        // Arrange
        var entity = new MockEntity(1);
        var otherObject = new MockTenantEntity(1, 1);

        // Act
        var result = entity.Equals(otherObject);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenNull()
    {
        // Arrange
        var entity = new MockEntity(1);

        // Act
        var result = entity.Equals(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenEqualEntities()
    {
        // Arrange
        var entity1 = new MockEntity(1);
        var entity2 = new MockEntity(1);

        // Act
        var result = entity1.Equals(entity2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameInstance()
    {
        // Arrange
        var entity = new MockEntity(1);

        // Act
        var result = entity.Equals(entity);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_TenantIdentity_IsSymmetric()
    {
        MockTenantEntity first = new(42, 1);
        MockTenantEntity same = new(42, 1);
        MockTenantEntity other = new(42, 2);

        Assert.True(first.Equals(same));
        Assert.True(same.Equals(first));
        Assert.Equal(first.GetHashCode(), same.GetHashCode());
        Assert.False(first.Equals(other));
        Assert.False(other.Equals(first));
    }

    [Fact]
    public void GetHashCode_ShouldReturnSameValue_WhenEqualEntities()
    {
        // Arrange
        var entity1 = new MockEntity(1);
        var entity2 = new MockEntity(1);

        // Act
        var hashCode1 = entity1.GetHashCode();
        var hashCode2 = entity2.GetHashCode();

        // Assert
        Assert.Equal(hashCode1, hashCode2);
    }

    [Fact]
    public void HashSet_AssignedEqualIds_DeduplicatesEntities()
    {
        MockEntity first = new(42);
        MockEntity second = new(42);

        Assert.True(first.Equals((object)second));
        Assert.Single(new HashSet<MockEntity> { first, second });
    }

    [Fact]
    public void Id_Should_Have_Value()
    {
        // Arrange
        var entity = new MockEntity(1);
        const KeyType expectedId = 1;

        // Act entity.Id = expectedId;

        // Assert
        Assert.Equal(expectedId, entity.Id);
    }

    [Fact]
    public void InequalityOperator_Should_Return_False_For_Same_Id()
    {
        // Arrange
        var entity1 = new MockEntity(1);
        var entity2 = new MockEntity(1);

        // Act
        bool result = entity1 != entity2;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void InequalityOperator_Should_Return_True_For_Different_Id()
    {
        // Arrange
        var entity1 = new MockEntity(1);
        var entity2 = new MockEntity(2);

        // Act
        bool result = entity1 != entity2;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDeleted_Should_Be_False_By_Default()
    {
        // Arrange
        var entity = new MockEntity(1);

        // Act

        // Assert
        Assert.False(entity.IsDeleted);
    }

    [Fact]
    public void IsNew_Should_Return_False_For_Non_Default_Id()
    {
        // Arrange
        var entity = new MockEntity(1);

        // Act
        bool result = entity.IsNew();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsNew_Should_Return_True_For_Default_Id()
    {
        // Arrange
        var entity = new MockEntity();

        // Act
        bool result = entity.IsNew();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void SetConcurrencyStamp_Should_Be_SetValue()
    {
        // Arrange
        var entity = new MockEntity { Id = 1, ConcurrencyStamp = "test" };

        // Assert
        Assert.NotNull(entity.ConcurrencyStamp);
        Assert.Equal("test", entity.ConcurrencyStamp);
    }

    [Fact]
    public void SetIsDeleted_Should_Be_SetValue()
    {
        // Arrange
        var entity = new MockEntity(1)
        {
            // Act
            IsDeleted = true
        };

        // Assert
        Assert.True(entity.IsDeleted);
    }

    [Fact]
    public void ToString_ShouldReturnExpectedString()
    {
        // Arrange
        var entity = new MockEntity(1);

        // Act
        var result = entity.ToString();

        // Assert
        Assert.Equal("[ENTITY: MockEntity] Key = 1", result);
    }

    #endregion Public Methods
}
