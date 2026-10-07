using Craft.Domain.Abstractions;

namespace Craft.Domain.Tests.Abstractions;

public class HasIdTests
{
    #region Private Classes

    private class ConcreteHasId : IHasId
    {
        #region Public Properties

        public KeyType Id { get; set; }

        #endregion Public Properties
    }

    #endregion Private Classes

    #region Public Methods

    [Fact]
    public void IHasId_CastsToIHasIdOfConcreteType()
    {
        // Arrange
        ConcreteHasId instance = new();

        // Act & Assert
        IHasId castInstance = instance;
        Assert.NotNull(castInstance);
    }

    [Fact]
    public void IHasId_ColumnName_IsConstant()
    {
        // Assert
        Assert.Equal("Id", IHasId.ColumnName);
    }

    [Fact]
    public void IHasId_GetId_ReturnsIdValue()
    {
        // Arrange
        IHasId instance = new ConcreteHasId { Id = 456 };

        // Act & Assert
        KeyType actualId = instance.GetId();
        Assert.Equal((KeyType)456, actualId);
    }

    [Fact]
    public void IHasId_IsNew_ReturnsFalseForNonDefaultId()
    {
        // Arrange
        IHasId instance = new ConcreteHasId { Id = 123 };

        // Act & Assert
        Assert.False(instance.IsNew);
    }

    [Fact]
    public void IHasId_IsNew_ReturnsTrueForDefaultId()
    {
        // Arrange
        IHasId instance = new ConcreteHasId();

        // Act & Assert
        Assert.True(instance.IsNew);
    }

    [Fact]
    public void IHasId_SetId_SetsIdValue()
    {
        // Arrange
        IHasId instance = new ConcreteHasId();

        // Act
        instance.SetId(789);

        // Assert
        Assert.Equal((KeyType)789, instance.Id);
    }

    #endregion Public Methods
}
