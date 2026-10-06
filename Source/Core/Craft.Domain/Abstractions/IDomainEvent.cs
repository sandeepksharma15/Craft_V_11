namespace Craft.Domain.Abstractions;

/// <summary>
/// Marker interface for domain events. Domain events represent something significant that happened
/// in the domain.
/// </summary>
public interface IDomainEvent
{
    #region Public Properties

    /// <summary>
    /// Gets the unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the type name of the event for serialization and routing purposes.
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// Gets the UTC timestamp when the event occurred.
    /// </summary>
    DateTime OccurredOnUtc { get; }

    #endregion Public Properties
}
