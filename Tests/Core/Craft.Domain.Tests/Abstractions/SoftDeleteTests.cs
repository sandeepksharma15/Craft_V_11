using Craft.Domain.Abstractions;

namespace Craft.Domain.Tests.Abstractions;

public class SoftDeleteTests
{
    #region Private Classes

    private class ConcreteSoftDelete : ISoftDelete
    {
        #region Public Properties

        public bool IsDeleted { get; set; }

        #endregion Public Properties
    }

    #endregion Private Classes

    #region Public Methods

    [Fact]
    public void Delete_SetsIsDeletedToTrue()
    {
        // Arrange
        ISoftDelete instance = new ConcreteSoftDelete();

        // Act
        instance.Delete();

        // Assert
        Assert.True(instance.IsDeleted);
    }

    [Fact]
    public void IsDeleted_ReturnsFalseInitially()
    {
        // Arrange
        ISoftDelete instance = new ConcreteSoftDelete();

        // Act & Assert
        Assert.False(instance.IsDeleted);
    }

    [Fact]
    public void ISoftDelete_ColumnName_IsConstant()
    {
        // Assert
        Assert.Equal("IsDeleted", ISoftDelete.ColumnName);
    }

    [Fact]
    public void Restore_HasNoEffectOnAlreadyRestoredObject()
    {
        // Arrange
        ISoftDelete instance = new ConcreteSoftDelete();

        // Act & Assert
        instance.Restore(); // Should have no effect

        // Assert
        Assert.False(instance.IsDeleted); // Remains false
    }

    [Fact]
    public void Restore_SetsIsDeletedToFalse()
    {
        // Arrange
        ISoftDelete instance = new ConcreteSoftDelete();
        instance.Delete(); // Set IsDeleted to true first

        // Act
        instance.Restore();

        // Assert
        Assert.False(instance.IsDeleted); // Should be restored to false
    }

    #endregion Public Methods
}
