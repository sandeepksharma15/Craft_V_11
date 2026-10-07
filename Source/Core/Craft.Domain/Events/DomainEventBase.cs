using Craft.Domain.Abstractions;

namespace Craft.Domain.Events;

/// <summary>Immutable event identity and UTC occurrence metadata.</summary>
/// <remarks>Equality uses EventId only; restoring an event must preserve its original ID.</remarks>
public abstract class DomainEventBase : IDomainEvent, IEquatable<DomainEventBase>
{
    protected DomainEventBase() : this(Guid.NewGuid(), DateTime.UtcNow) { }

    protected DomainEventBase(DateTime occurredOnUtc) : this(Guid.NewGuid(), occurredOnUtc) { }

    /// <summary>Restores metadata for a persisted event without generating a new identity.</summary>
    protected DomainEventBase(Guid eventId, DateTime occurredOnUtc)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("Event ID cannot be empty.", nameof(eventId));

        if (occurredOnUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Event timestamp must be UTC.", nameof(occurredOnUtc));

        EventId = eventId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid EventId { get; }

    public DateTime OccurredOnUtc { get; }

    /// <summary>Defaults to the CLR type name; override for a stable external contract name.</summary>
    public virtual string EventType => GetType().Name;

    public Guid? CorrelationId { get; init; }

    public Guid? CausationId { get; init; }

    public static bool operator ==(DomainEventBase? left, DomainEventBase? right)
        => Equals(left, right);

    public static bool operator !=(DomainEventBase? left, DomainEventBase? right)
        => !Equals(left, right);

    public bool Equals(DomainEventBase? other)
        => other is not null && EventId == other.EventId;

    public override bool Equals(object? obj)
        => obj is DomainEventBase other && Equals(other);

    public override int GetHashCode()
        => EventId.GetHashCode();

    public override string ToString()
        => $"{EventType} ({EventId}) at {OccurredOnUtc:O}";
}
