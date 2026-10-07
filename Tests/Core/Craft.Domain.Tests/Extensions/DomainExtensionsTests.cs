using Craft.Domain.Abstractions;
using Craft.Domain.Base;

namespace Craft.Domain.Tests.Extensions;

public class DomainExtensionsTests
{
    [Theory]
    [InlineData(0L, true)]
    [InlineData(42L, false)]
    [InlineData(-1L, false)]
    [InlineData(long.MinValue, false)]
    [InlineData(long.MaxValue, false)]
    public void IdentifierChecks_LongKeys_UseDefaultAndAssignedValues(long key, bool isDefault)
        => AssertKeyBehavior(key, 17L, isDefault);

    [Theory]
    [InlineData(null, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(42, false)]
    public void IdentifierChecks_NullableKeys_TreatOnlyNullAsDefault(int? key, bool isDefault)
        => AssertKeyBehavior(key, (int?)17, isDefault);

    [Theory]
    [InlineData(null, true)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("tenant-a", false)]
    public void IdentifierChecks_StringKeys_UseOrdinalEquality(string? key, bool isDefault)
        => AssertKeyBehavior(key, "TENANT-A", isDefault);

    [Fact]
    public void IdentifierChecks_GuidKeys_UseDefaultAndAssignedValues()
    {
        var key = Guid.NewGuid();
        var other = Guid.NewGuid();

        AssertKeyBehavior(Guid.Empty, other, true);
        AssertKeyBehavior(key, other, false);
    }

    [Fact]
    public void IdentifierChecks_CustomKeys_UseTypedEquality()
    {
        AssertKeyBehavior(default(EquatableKey), new EquatableKey(17), true);
        AssertKeyBehavior(new EquatableKey(42), new EquatableKey(17), false);
    }

    [Fact]
    public void IdentifierChecks_DefaultContracts_InferConfiguredKeyType()
    {
        var holder = new DefaultKeyHolder();
        IEntity entity = holder;
        IHasTenant tenant = holder;
        IHasUser user = holder;

        Assert.True(entity.IsNullOrDefault());
        Assert.False(tenant.BelongsToTenant(default));
        Assert.False(user.BelongsToUser(default));
    }

    [Fact]
    public void IsNullOrDefault_DtoWithAssignedNegativeId_ReturnsFalse()
    {
        var dto = new TestDto { Id = -42 };

        Assert.False(dto.IsNullOrDefault());
    }

    [Fact]
    public void IdentifierChecks_NullableFlow_RecognizesNonNullReceiver()
        => AssertNullableFlow(new KeyHolder<int> { Id = 42, TenantId = 42, UserId = 42 });

    private static void AssertNullableFlow(KeyHolder<int>? holder)
    {
        if (!holder.IsNullOrDefault())
            Assert.Equal(42, holder.Id);
        if (holder.BelongsToTenant(42))
            Assert.Equal(42, holder.TenantId);
        if (holder.BelongsToUser(42))
            Assert.Equal(42, holder.UserId);
    }

    private static void AssertKeyBehavior<TKey>(TKey key, TKey other, bool isDefault)
    {
        var holder = new KeyHolder<TKey> { Id = key, TenantId = key, UserId = key };
        IHasId<TKey>? missingId = null;
        IHasTenant<TKey>? missingTenant = null;
        IHasUser<TKey>? missingUser = null;

        Assert.True(missingId.IsNullOrDefault());
        Assert.False(missingTenant.BelongsToTenant(key));
        Assert.False(missingUser.BelongsToUser(key));
        Assert.Equal(isDefault, holder.IsNullOrDefault());
        Assert.Equal(!isDefault, holder.BelongsToTenant(key));
        Assert.Equal(!isDefault, holder.BelongsToUser(key));
        Assert.False(holder.BelongsToTenant(other));
        Assert.False(holder.BelongsToUser(other));
        Assert.False(holder.BelongsToTenant(default!));
        Assert.False(holder.BelongsToUser(default!));
        Assert.Equal(key, holder.Id);
        Assert.Equal(key, holder.TenantId);
        Assert.Equal(key, holder.UserId);
    }

    private sealed class KeyHolder<TKey> : IEntity<TKey>, IHasTenant<TKey>, IHasUser<TKey>
    {
        public TKey Id { get; set; } = default!;
        public TKey TenantId { get; set; } = default!;
        public TKey UserId { get; set; } = default!;
    }

    private sealed class DefaultKeyHolder : IEntity, IHasTenant, IHasUser
    {
        public KeyType Id { get; set; }
        public KeyType TenantId { get; set; }
        public KeyType UserId { get; set; }
    }

    private sealed class TestDto : BaseDTO<int>;

    private readonly struct EquatableKey(int value) : IEquatable<EquatableKey>
    {
        private int Value { get; } = value;

        public bool Equals(EquatableKey other) => Value == other.Value;
        public override bool Equals(object? obj) => throw new InvalidOperationException("Use typed equality.");
        public override int GetHashCode() => Value.GetHashCode();
    }
}