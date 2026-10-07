using Craft.Domain.Abstractions;

namespace Craft.Domain.Tests.Abstractions;

public class HasTenantTests
{
    #region Private Classes

    private class ConcreteHasTenant : IHasTenant
    {
        #region Public Properties

        public KeyType Id { get; set; }
        public KeyType TenantId { get; set; }

        #endregion Public Properties
    }

    #endregion Private Classes

    #region Public Methods

    [Fact]
    public void IHasTenant_CastsToIHasTenantOfConcreteType()
    {
        // Arrange
        ConcreteHasTenant instance = new();

        // Act & Assert
        IHasTenant castInstance = instance;
        Assert.IsType<ConcreteHasTenant>(castInstance);
    }

    [Fact]
    public void IHasTenant_ColumnName_IsConstant()
    {
        // Assert
        Assert.Equal(IHasTenant.ColumnName, "TenantId");
    }

    [Fact]
    public void IHasTenantOfTKey_GetTenantId_ReturnsTenantIdValue()
    {
        // Arrange
        ConcreteHasTenant instance = new() { TenantId = 123 };
        IHasTenant castInstance = instance;

        // Act & Assert
        var actualId = castInstance.GetTenantId();
        Assert.Equal((KeyType)123, actualId);
    }

    [Fact]
    public void IHasTenantOfTKey_IsTenantIdSet_ReturnsFalseForDefaultTenantId()
    {
        // Arrange
        IHasTenant castInstance = (ConcreteHasTenant)new();

        // Act & Assert
        bool isSet = castInstance.IsTenantIdSet();
        Assert.False(isSet);
    }

    [Fact]
    public void IHasTenantOfTKey_IsTenantIdSet_ReturnsTrueForSetTenantId()
    {
        // Arrange
        IHasTenant castInstance = (ConcreteHasTenant)new() { TenantId = 123 };

        // Act & Assert
        bool isSet = castInstance.IsTenantIdSet();
        Assert.True(isSet);
    }

    [Fact]
    public void IHasTenantOfTKey_SetTenantId_SetsTenantIdValue()
    {
        // Arrange
        ConcreteHasTenant instance = new() { TenantId = 123 };
        IHasTenant castInstance = instance;

        // Act
        castInstance.SetTenantId(456);

        // Assert
        Assert.Equal((KeyType)456, instance.TenantId);
    }

    #endregion Public Methods
}
