using Craft.Domain.Abstractions;

namespace Craft.Domain.Base;

/// <summary>Convenience base for entities opting into the aggregate root marker.</summary>
public abstract class AggregateRoot<TKey> : BaseEntity<TKey>, IAggregateRoot<TKey>
{
    protected AggregateRoot() { }

    protected AggregateRoot(TKey id) : base(id) { }
}

/// <summary>Aggregate root base with a long identifier.</summary>
public abstract class AggregateRoot : AggregateRoot<long>, IAggregateRoot
{
    protected AggregateRoot() { }

    protected AggregateRoot(long id) : base(id) { }
}
