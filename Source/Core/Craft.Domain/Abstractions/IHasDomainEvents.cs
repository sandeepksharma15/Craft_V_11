namespace Craft.Domain.Abstractions;

/// <summary>
/// Defines a contract for entities that can raise domain events.
/// </summary>
public interface IHasDomainEvents
{
    #region Public Properties

    /// <summary>
    /// Gets the collection of domain events raised by this entity.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Adds a domain event to the collection.
    /// </summary>
    /// <param name="domainEvent"> The domain event to add. </param>
    void AddDomainEvent(IDomainEvent domainEvent);

    /// <summary>
    /// Clears all domain events from the collection.
    /// </summary>
    void ClearDomainEvents();

    /// <summary>
    /// Removes a domain event from the collection.
    /// </summary>
    /// <param name="domainEvent"> The domain event to remove. </param>
    /// <returns> True if the event was removed; otherwise, false. </returns>
    bool RemoveDomainEvent(IDomainEvent domainEvent);

    #endregion Public Methods
}

/// <summary>
/// Provides a default implementation for managing domain events.
/// </summary>
/// <remarks>
/// This class can be used as a composition helper for entities that need domain event support but
/// cannot inherit from a base class that provides it.
/// </remarks>
public sealed class DomainEventCollection : IHasDomainEvents
{
    #region Private Fields

    private readonly List<IDomainEvent> _domainEvents = [];

    #endregion Private Fields

    #region Public Properties

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

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
