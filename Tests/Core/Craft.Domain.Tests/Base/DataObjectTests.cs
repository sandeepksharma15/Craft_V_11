using Craft.Domain.Abstractions;

namespace Craft.Domain.Tests.Base;

public class DataObjectTests
{
    #region Public Methods

    [Theory]
    [InlineData(typeof(BaseDtoTests.TestDto))]
    [InlineData(typeof(BaseVmTests.TestVm))]
    [InlineData(typeof(BaseModelTests.TestDto))]
    public void MutableModel_IdenticalValues_KeepReferenceIdentity(Type modelType)
    {
        IDataObject first = (IDataObject)Activator.CreateInstance(modelType)!;
        IDataObject second = (IDataObject)Activator.CreateInstance(modelType)!;

        first.Id = second.Id = 42;
        Assert.False(first.Equals(second));
        Assert.True(first.Equals(first));

        first.IsDeleted = true;
        Assert.False(second.IsDeleted);
        first.Id = 43;
        Assert.Equal((KeyType)42, second.Id);
    }

    [Theory]
    [InlineData(typeof(BaseDtoTests.TestDto))]
    [InlineData(typeof(BaseVmTests.TestVm))]
    [InlineData(typeof(BaseModelTests.TestDto))]
    public void MutableModel_JsonRoundTrip_PreservesState(Type modelType)
    {
        IDataObject original = (IDataObject)Activator.CreateInstance(modelType)!;
        original.Id = 42;
        original.ConcurrencyStamp = "stamp";
        original.IsDeleted = true;

        string json = JsonSerializer.Serialize(original, modelType);
        IDataObject restored = (IDataObject)JsonSerializer.Deserialize(json, modelType)!;

        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.ConcurrencyStamp, restored.ConcurrencyStamp);
        Assert.Equal(original.IsDeleted, restored.IsDeleted);
        Assert.NotSame(original, restored);
        Assert.False(original.Equals(restored));
        Assert.IsType<IModel>(restored, exactMatch: false);
    }

    #endregion Public Methods
}
