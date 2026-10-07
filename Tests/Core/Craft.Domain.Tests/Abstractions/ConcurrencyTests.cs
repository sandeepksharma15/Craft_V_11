using Craft.Domain.Abstractions;

namespace Craft.Domain.Tests.Abstractions;

public class ConcurrencyTests
{
    #region Private Classes

    private class TestEntity : IHasConcurrency
    {
        #region Public Properties

        public string? ConcurrencyStamp { get; set; }

        #endregion Public Properties
    }

    #endregion Private Classes

    #region Public Methods

    [Fact]
    public void ClearConcurrencyStamp_Should_Set_ConcurrencyStamp_To_Null()
    {
        // Arrange
        IHasConcurrency entity = new TestEntity();
        entity.SetConcurrencyStamp("test-stamp");
        Assert.NotNull(entity.ConcurrencyStamp); // Ensure it's set

        // Act
        entity.ClearConcurrencyStamp();

        // Assert
        Assert.Null(entity.ConcurrencyStamp);
    }

    [Fact]
    public void GetConcurrencyStamp_Should_ReturnStamp()
    {
        // Arrange
        IHasConcurrency entity = new TestEntity();
        entity.SetConcurrencyStamp("test-stamp");

        // Act
        string? stamp = entity.GetConcurrencyStamp();

        // Assert
        Assert.Equal("test-stamp", stamp);
    }

    [Fact]
    public void HasConcurrencyStamp_WithoutStamp_Should_ReturnFalse()
    {
        // Arrange
        IHasConcurrency entity = new TestEntity();

        // Act
        bool hasStamp = entity.HasConcurrencyStamp();

        // Assert
        Assert.False(hasStamp);
    }

    [Fact]
    public void HasConcurrencyStamp_WithStamp_Should_ReturnTrue()
    {
        // Arrange
        IHasConcurrency entity = new TestEntity();
        entity.SetConcurrencyStamp("test-stamp");

        // Act
        bool hasStamp = entity.HasConcurrencyStamp();

        // Assert
        Assert.True(hasStamp);
    }

    [Fact]
    public void SetConcurrencyStamp_Should_SetStamp()
    {
        // Arrange
        IHasConcurrency entity = new TestEntity();

        // Act
        entity.SetConcurrencyStamp("test-stamp");

        // Assert
        Assert.Equal("test-stamp", entity.ConcurrencyStamp);
    }

    [Fact]
    public void SetConcurrencyStamp_WithNull_Should_GenerateNewStamp()
    {
        // Arrange
        IHasConcurrency entity = new TestEntity();

        // Act
        entity.SetConcurrencyStamp(null);

        // Assert
        Assert.NotNull(entity.ConcurrencyStamp);
    }

    #endregion Public Methods
}
