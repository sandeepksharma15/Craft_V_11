using System.Text.Json.Serialization;
using Craft.Domain.Abstractions;
using Craft.Domain.Events;

namespace Craft.Domain.Tests.Events;

public class DomainEventBaseTests
{
    #region Test Implementations

    private sealed class AnotherDomainEvent : DomainEventBase
    {
        #region Public Constructors

        public AnotherDomainEvent(int value) => Value = value;

        public AnotherDomainEvent(int value, Guid eventId, DateTime occurredOnUtc) : base(eventId, occurredOnUtc)
            => Value = value;

        #endregion Public Constructors

        #region Public Properties

        public override string EventType => "another-event.v1";
        public int Value { get; }

        #endregion Public Properties
    }

    private sealed class TestDomainEvent : DomainEventBase
    {
        #region Public Constructors

        public TestDomainEvent(string data) => Data = data;

        public TestDomainEvent(string data, DateTime occurredOnUtc) : base(occurredOnUtc)
            => Data = data;

        [JsonConstructor]
        public TestDomainEvent(string data, Guid eventId, DateTime occurredOnUtc) : base(eventId, occurredOnUtc)
            => Data = data;

        #endregion Public Constructors

        #region Public Properties

        public string Data { get; }

        #endregion Public Properties
    }

    #endregion Test Implementations

    #region Public Methods

    [Fact]
    public void Constructor_EmptyRestoredId_RejectsInvalidIdentity()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new TestDomainEvent("data", Guid.Empty, DateTime.UtcNow));

        Assert.Equal("eventId", exception.ParamName);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Constructor_NonUtcTimestamp_RejectsAmbiguousTime(DateTimeKind kind)
    {
        DateTime timestamp = new(2026, 10, 7, 9, 0, 0, kind);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new TestDomainEvent("data", timestamp));

        Assert.Equal("occurredOnUtc", exception.ParamName);
        Assert.Throws<ArgumentException>(() => new TestDomainEvent("data", Guid.NewGuid(), timestamp));
    }

    [Fact]
    public void Equality_NullAndUnrelatedObjects_ReturnFalse()
    {
        TestDomainEvent domainEvent = new("data");
        DomainEventBase? missing = null;

        Assert.False(domainEvent.Equals(new object()));
        Assert.False(domainEvent == missing);
        Assert.False(missing == domainEvent);
        Assert.True(domainEvent != missing);
        Assert.True(missing != domainEvent);
        Assert.Null(missing);
    }

    [Fact]
    public void EventType_Override_IsVisibleThroughContractAndDiagnostics()
    {
        AnotherDomainEvent domainEvent = new(42);
        IDomainEvent contract = domainEvent;

        Assert.Equal("another-event.v1", contract.EventType);
        Assert.StartsWith("another-event.v1", domainEvent.ToString());
        Assert.Contains(domainEvent.OccurredOnUtc.ToString("O"), domainEvent.ToString());
    }

    [Fact]
    public void JsonRoundTrip_PreservesIdentityMetadataAndPayload()
    {
        TestDomainEvent original = new("payload")
        {
            CorrelationId = Guid.NewGuid(),
            CausationId = Guid.NewGuid()
        };

        string json = JsonSerializer.Serialize(original);
        TestDomainEvent restored = JsonSerializer.Deserialize<TestDomainEvent>(json)!;

        Assert.NotNull(restored);
        Assert.Equal(original.EventId, restored.EventId);
        Assert.Equal(original.OccurredOnUtc, restored.OccurredOnUtc);
        Assert.Equal(DateTimeKind.Utc, restored.OccurredOnUtc.Kind);
        Assert.Equal(original.CorrelationId, restored.CorrelationId);
        Assert.Equal(original.CausationId, restored.CausationId);
        Assert.Equal(original.Data, restored.Data);
        Assert.True(original.Equals(restored));
    }

    [Fact]
    public void RestoredEvent_EqualityDependsOnlyOnEventId()
    {
        TestDomainEvent original = new("data");
        AnotherDomainEvent restored = new(42, original.EventId, original.OccurredOnUtc.AddSeconds(1));

        Assert.True(original.Equals(restored));
        Assert.True(restored.Equals(original));
        Assert.Equal(original.GetHashCode(), restored.GetHashCode());
    }

    [Fact]
    public void RestoredEvent_SameId_PreservesEqualityAndHashCollections()
    {
        TestDomainEvent original = new("original");
        TestDomainEvent restored = new("restored", original.EventId, original.OccurredOnUtc);

        Assert.Equal(original.EventId, restored.EventId);
        Assert.Equal(original.OccurredOnUtc, restored.OccurredOnUtc);
        Assert.NotSame(original, restored);
        Assert.True(original.Equals(restored));
        Assert.True(restored.Equals((object)original));
        Assert.True(original == restored);
        Assert.False(original != restored);
        Assert.Equal(original.GetHashCode(), restored.GetHashCode());
        Assert.Single(new HashSet<DomainEventBase> { original, restored });
    }

    #endregion Public Methods

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldGenerateUniqueEventId()
    {
        // Arrange & Act
        var event1 = new TestDomainEvent("test1");
        var event2 = new TestDomainEvent("test2");

        // Assert
        Assert.NotEqual(Guid.Empty, event1.EventId);
        Assert.NotEqual(Guid.Empty, event2.EventId);
        Assert.NotEqual(event1.EventId, event2.EventId);
    }

    [Fact]
    public void Constructor_ShouldSetOccurredOnUtcToCurrentTime()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var domainEvent = new TestDomainEvent("test");

        // Assert
        var after = DateTime.UtcNow;
        Assert.InRange(domainEvent.OccurredOnUtc, before, after);
    }

    [Fact]
    public void Constructor_WithTimestamp_ShouldUseProvidedTimestamp()
    {
        // Arrange
        var specificTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var domainEvent = new TestDomainEvent("test", specificTime);

        // Assert
        Assert.Equal(specificTime, domainEvent.OccurredOnUtc);
    }

    #endregion Constructor Tests

    #region EventType Tests

    [Fact]
    public void EventType_ShouldBeDifferentForDifferentEventTypes()
    {
        // Arrange & Act
        var event1 = new TestDomainEvent("test");
        var event2 = new AnotherDomainEvent(42);

        // Assert
        Assert.NotEqual(event1.EventType, event2.EventType);
    }

    [Fact]
    public void EventType_ShouldReturnClassName()
    {
        // Arrange & Act
        var domainEvent = new TestDomainEvent("test");

        // Assert
        Assert.Equal("TestDomainEvent", domainEvent.EventType);
    }

    #endregion EventType Tests

    #region CorrelationId and CausationId Tests

    [Fact]
    public void CausationId_CanBeSetViaInitProperty()
    {
        // Arrange
        var causationId = Guid.NewGuid();

        // Act
        var domainEvent = new TestDomainEvent("test") { CausationId = causationId };

        // Assert
        Assert.Equal(causationId, domainEvent.CausationId);
    }

    [Fact]
    public void CausationId_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var domainEvent = new TestDomainEvent("test");

        // Assert
        Assert.Null(domainEvent.CausationId);
    }

    [Fact]
    public void CorrelationId_CanBeSetViaInitProperty()
    {
        // Arrange
        var correlationId = Guid.NewGuid();

        // Act
        var domainEvent = new TestDomainEvent("test") { CorrelationId = correlationId };

        // Assert
        Assert.Equal(correlationId, domainEvent.CorrelationId);
    }

    [Fact]
    public void CorrelationId_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var domainEvent = new TestDomainEvent("test");

        // Assert
        Assert.Null(domainEvent.CorrelationId);
    }

    #endregion CorrelationId and CausationId Tests

    #region Equality Tests

    [Fact]
    public void EqualityOperator_ShouldReturnTrue_ForBothNull()
    {
        // Arrange
        DomainEventBase? event1 = null;
        DomainEventBase? event2 = null;

        // Act & Assert
        Assert.True(event1 == event2);
    }

    [Fact]
    public void EqualityOperator_ShouldReturnTrue_ForSameInstance()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("test");
        var sameReference = domainEvent;

        // Act & Assert
        Assert.True(domainEvent == sameReference);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_ForDifferentEvents()
    {
        // Arrange
        var event1 = new TestDomainEvent("test");
        var event2 = new TestDomainEvent("test");

        // Act & Assert (different EventIds)
        Assert.False(event1.Equals(event2));
    }

    [Fact]
    public void Equals_ShouldReturnFalse_ForNull()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("test");

        // Act & Assert
        Assert.False(domainEvent.Equals(null));
    }

    [Fact]
    public void Equals_ShouldReturnTrue_ForSameInstance()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("test");

        // Act & Assert
        Assert.True(domainEvent.Equals(domainEvent));
    }

    [Fact]
    public void InequalityOperator_ShouldReturnTrue_ForDifferentEvents()
    {
        // Arrange
        var event1 = new TestDomainEvent("test");
        var event2 = new TestDomainEvent("test");

        // Act & Assert
        Assert.True(event1 != event2);
    }

    #endregion Equality Tests

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_ShouldBeConsistent()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("test");

        // Act
        var hash1 = domainEvent.GetHashCode();
        var hash2 = domainEvent.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    #endregion GetHashCode Tests

    #region ToString Tests

    [Fact]
    public void ToString_ShouldIncludeEventTypeAndEventId()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("test");

        // Act
        var result = domainEvent.ToString();

        // Assert
        Assert.Contains("TestDomainEvent", result);
        Assert.Contains(domainEvent.EventId.ToString(), result);
    }

    #endregion ToString Tests

    #region IDomainEvent Interface Tests

    [Fact]
    public void DomainEventBase_ShouldImplementIDomainEvent()
    {
        // Arrange & Act
        var domainEvent = new TestDomainEvent("test");

        // Assert
        Assert.IsType<IDomainEvent>(domainEvent, exactMatch: false);
    }

    [Fact]
    public void IDomainEvent_Properties_ShouldBeAccessible()
    {
        // Arrange
        IDomainEvent domainEvent = new TestDomainEvent("test");

        // Assert
        Assert.NotEqual(Guid.Empty, domainEvent.EventId);
        Assert.NotEqual(default, domainEvent.OccurredOnUtc);
        Assert.Equal("TestDomainEvent", domainEvent.EventType);
    }

    #endregion IDomainEvent Interface Tests
}
