using System.Collections.ObjectModel;

namespace Craft.Domain.Abstractions;

/// <summary>
/// Defines a contract for entities that can raise domain events.
/// </summary>
public interface IHasDomainEvents
{
    #region Public Properties

    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Adds a domain event to the collection.
    /// </summary>
    void AddDomainEvent(IDomainEvent domainEvent);

    void ClearDomainEvents();

    /// <summary>
    /// Removes a domain event from the collection.
    /// </summary>
    bool RemoveDomainEvent(IDomainEvent domainEvent);

    #endregion Public Methods
}

/// <summary>
/// Ordered event buffer with a live read-only view. Not thread-safe.
/// </summary>
public sealed class DomainEventCollection : IHasDomainEvents
{
    #region Private Fields

    private readonly List<IDomainEvent> _domainEvents = [];
    private readonly ReadOnlyCollection<IDomainEvent> _view;

    #endregion Private Fields

    #region Public Constructors

    public DomainEventCollection()
    {
        _view = _domainEvents.AsReadOnly();
    }

    #endregion Public Constructors

    #region Public Properties

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _view;

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc />
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        _domainEvents.Add(domainEvent);
    }

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <inheritdoc />
    public bool RemoveDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return _domainEvents.Remove(domainEvent);
    }

    #endregion Public Methods
}
