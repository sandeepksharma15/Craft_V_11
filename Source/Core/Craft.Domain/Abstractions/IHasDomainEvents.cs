using System.Collections.ObjectModel;

namespace Craft.Domain.Abstractions;

/// <summary>Defines a contract for entities that can raise domain events.</summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Adds a domain event to the collection.
    /// </summary>
    void AddDomainEvent(IDomainEvent domainEvent);

    /// <summary>
    /// Removes a domain event from the collection.
    /// </summary>
    bool RemoveDomainEvent(IDomainEvent domainEvent);

    void ClearDomainEvents();
}

/// <summary>Ordered event buffer with a live read-only view. Not thread-safe.</summary>
public sealed class DomainEventCollection : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];
    private readonly ReadOnlyCollection<IDomainEvent> _view;

    public DomainEventCollection()
    {
        _view = _domainEvents.AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _view;

    /// <inheritdoc />
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <inheritdoc />
    public bool RemoveDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        return _domainEvents.Remove(domainEvent);
    }

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();
}
