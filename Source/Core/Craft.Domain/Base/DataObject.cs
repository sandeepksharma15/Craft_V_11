using Craft.Domain.Abstractions;

namespace Craft.Domain.Base;

/// <summary>Shared mutable state for DTOs, view models, and data models.</summary>
public abstract class DataObject<TKey> : IDataObject<TKey>
{
    public virtual TKey Id { get; set; } = default!;

    public virtual string? ConcurrencyStamp { get; set; }

    public virtual bool IsDeleted { get; set; }
}
