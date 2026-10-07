using System.ComponentModel.DataAnnotations;
using Craft.Domain.Abstractions;

namespace Craft.Domain.Base;

/// <summary>Entity base with the configured default identifier.</summary>
public abstract class BaseEntity : BaseEntity<KeyType>, IEntity, IModel
{
    protected BaseEntity() { }

    protected BaseEntity(KeyType id) : base(id) { }
}

/// <summary>Entity identity, concurrency state, and soft-delete state.</summary>
/// <remarks>Configure key generation in persistence. Do not change IDs while entities are stored in hash collections.</remarks>
public abstract class BaseEntity<TKey> : IEntity<TKey>, IHasConcurrency, ISoftDelete, IModel<TKey>, IEquatable<BaseEntity<TKey>>
{
    protected BaseEntity() { }

    protected BaseEntity(TKey id) { Id = id; }

    [Key]
    public virtual TKey Id { get; set; } = default!;

    [ConcurrencyCheck]
    [MaxLength(IHasConcurrency.MaxLength)]
    public virtual string? ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    public virtual bool IsDeleted { get; set; }

    public static bool operator ==(BaseEntity<TKey>? left, BaseEntity<TKey>? right)
        => Equals(left, right);

    public static bool operator !=(BaseEntity<TKey>? left, BaseEntity<TKey>? right)
        => !Equals(left, right);

    public override bool Equals(object? obj)
        => obj is BaseEntity<TKey> other && Equals(other);

    /// <summary>Compares type and assigned ID; distinct entities with default IDs are never equal.</summary>
    public bool Equals(BaseEntity<TKey>? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        return !EqualityComparer<TKey>.Default.Equals(Id, default)
            && EqualityComparer<TKey>.Default.Equals(Id, other.Id)
            && AdditionalEqualityCheck(other);
    }

    /// <summary>Add identity criteria such as tenant or shard. Overrides must preserve symmetric equality.</summary>
    protected virtual bool AdditionalEqualityCheck(BaseEntity<TKey> other) => true;

    public override int GetHashCode()
        => HashCode.Combine(GetType(), Id);

    /// <summary>Checks for the default ID, not whether the entity has been persisted.</summary>
    public virtual bool IsNew()
        => EqualityComparer<TKey>.Default.Equals(Id, default);

    public override string ToString()
        => $"[ENTITY: {GetType().Name}] Key = {Id}";
}
