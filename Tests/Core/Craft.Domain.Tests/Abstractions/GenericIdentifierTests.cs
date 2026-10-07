using Craft.Domain.Abstractions;

namespace Craft.Domain.Tests.Abstractions;

public class GenericIdentifierTests
{
    #region Private Methods

    private static void AssertKeyBehavior<TKey>(TKey defaultValue, TKey assignedValue)
    {
        KeyHolder<TKey> holder = new();
        IHasId<TKey> entity = holder;
        IHasUser<TKey> user = holder;
        IHasTenant<TKey> tenant = holder;

        entity.SetId(defaultValue);
        user.SetUserId(defaultValue);
        tenant.SetTenantId(defaultValue);

        Assert.True(entity.IsNew);
        Assert.False(user.IsUserIdSet());
        Assert.False(tenant.IsTenantIdSet());

        entity.SetId(assignedValue);
        user.SetUserId(assignedValue);
        tenant.SetTenantId(assignedValue);

        Assert.False(entity.IsNew);
        Assert.True(user.IsUserIdSet());
        Assert.True(tenant.IsTenantIdSet());
        Assert.Equal(assignedValue, entity.GetId());
        Assert.Equal(assignedValue, user.GetUserId());
        Assert.Equal(assignedValue, tenant.GetTenantId());
    }

    #endregion Private Methods

    #region Private Classes

    private sealed class KeyHolder<TKey> : IHasId<TKey>, IHasUser<TKey>, IHasTenant<TKey>
    {
        #region Public Properties

        public TKey Id { get; set; } = default!;
        public TKey TenantId { get; set; } = default!;
        public TKey UserId { get; set; } = default!;

        #endregion Public Properties
    }

    #endregion Private Classes

    #region Public Methods

    [Fact]
    public void GuidKeys_SupportDefaultAndAssignedValues()
        => AssertKeyBehavior(Guid.Empty, Guid.NewGuid());

    [Fact]
    public void LongKeys_SupportDefaultAndAssignedValues()
        => AssertKeyBehavior(0L, 42L);

    [Fact]
    public void NullableKeys_TreatNullAsDefaultAndZeroAsAssigned()
        => AssertKeyBehavior<long?>(null, 0L);

    [Fact]
    public void ReferenceKeys_TreatEmptyStringAsAssigned()
        => AssertKeyBehavior<string?>(null, string.Empty);

    [Fact]
    public void ReferenceKeys_TreatNullAsDefaultWithoutThrowing()
        => AssertKeyBehavior<string?>(null, "device-1");

    #endregion Public Methods
}
