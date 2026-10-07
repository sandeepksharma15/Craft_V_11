# Craft.Domain

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A foundational Domain-Driven Design (DDD) library for .NET 10 applications. Provides base classes, contracts, and patterns for building robust domain models with proper identity, equality, concurrency control, and event-driven architecture support.

## Table of Contents

- [Installation](#installation)
- [Default Key Type](#default-key-type)
- [Features](#features)
- [Quick Start](#quick-start)
- [Architecture Overview](#architecture-overview)
- [Core Concepts](#core-concepts)
  - [Entities](#entities)
  - [Value Objects](#value-objects)
  - [Aggregate Roots](#aggregate-roots)
  - [Domain Events](#domain-events)
  - [Data Transfer Objects](#data-transfer-objects)
- [Contracts (Interfaces)](#contracts-interfaces)
- [Exception Handling](#exception-handling)
- [Localization](#localization)
- [Best Practices](#best-practices)
- [Related Projects](#related-projects)

## Installation

Add a reference to `Craft.Domain` in your project:

```xml
<ProjectReference Include="path/to/Craft.Domain.csproj" />
```

## Default Key Type

The repository's [`Directory.Build.props`](../../../Directory.Build.props) defines `CraftDefaultKeyType` (default: `System.Int64`) and generates `global using KeyType = System.Int64;` for its SDK-style C# projects. No per-project alias file is needed.

Non-generic contracts and bases follow that setting:

```csharp
public interface IModel : IModel<KeyType>;
public interface IEntity : IEntity<KeyType>;
```

`IModel<TKey>` marks identified types eligible for Craft model operations; it is independent of entity or aggregate status.

Change the central property to `System.Guid` and rebuild libraries and consumers to change the default. Generic types such as `BaseEntity<Guid>` remain available without changing the default. This is a compile-time choice, not a runtime setting or a consumer override of an existing package.

The alias does not configure EF key generation. Switching an existing database requires explicit schema/data migration and a compatible ID generation strategy; numeric auto-increment does not carry over to GUID keys. Existing numeric fixtures may also require updates. See the [repository configuration guide](../../../README.md#default-key-type) for build overrides and importing the shared configuration.

## Base Classes

`BaseEntity<TKey>` supplies identity, a concurrency stamp, and soft-delete state. `AggregateRoot<TKey>` adds the optional aggregate marker. Their non-generic counterparts use the configured `KeyType`; neither requires domain events.

`DataObject<TKey>`, `BaseDTO<TKey>`, `BaseVm<TKey>`, and `BaseModel<TKey>` are mutable classes. DTOs, editable view models, and general transfer models keep reference equality; they do not inherit entity equality. Existing derived records must become classes, and record `with` expressions must be replaced with explicit copying. These bases keep their existing `IDataObject`/`IModel`, concurrency, and soft-delete contracts. Independent immutable records can still be used without inheriting these bases.

Entity equality requires the same concrete runtime type and a non-default ID. Distinct entities with default IDs are unequal; the same reference is always equal. `AdditionalEqualityCheck` can include tenant or shard identity and must remain symmetric. Equality ignores concurrency and deletion state. Do not change IDs while an entity is a dictionary key or hash-set member. `IsNew()` checks only the default ID; a preassigned GUID does not prove that an entity is persisted.

The base keeps key and concurrency annotations but does not force a key-generation strategy or column order. Configure database-generated numeric keys or application-assigned IDs in the consuming persistence model. Review generated migrations when upgrading an existing model. EF conventions and provider behaviour still apply; no custom JSON or EF value converters are required for these primitive key types.

## Features

- ✅ **Base Entity Classes** - `BaseEntity<TKey>` with identity, concurrency, and soft-delete
- ✅ **Value Objects** - `ValueObject` and `SingleValueObject<T>` with structural equality
- ✅ **Aggregate Roots** - `IAggregateRoot` marker interface for DDD boundaries
- ✅ **Domain Events** - `IDomainEvent`, `DomainEventBase`, and `IHasDomainEvents`
- ✅ **Data Transfer Objects** - `BaseDTO`, `BaseVm`, `BaseModel` with `IDataObject`
- ✅ **Rich Exception Hierarchy** - Categorized exceptions with HTTP status codes
- ✅ **Localization Support** - Resource-backed error messages
- ✅ **Multi-tenancy Support** - `IHasTenant` interface
- ✅ **Optimistic Concurrency** - Built-in concurrency stamps

## Quick Start

### Define an Entity

```csharp
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}
```

### Define a Value Object

```csharp
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

### Define an Aggregate Root with Domain Events

```csharp
public class Order : BaseEntity, IAggregateRoot, IHasDomainEvents
{
    private readonly List<OrderLine> _lines = [];
    private readonly DomainEventCollection _events = new();
    
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _events.DomainEvents;
    
    public void AddLine(Product product, int quantity)
    {
        var line = new OrderLine(product.Id, quantity, product.Price);
        _lines.Add(line);
        AddDomainEvent(new OrderLineAddedEvent(Id, line));
    }
    
    public void AddDomainEvent(IDomainEvent domainEvent) => _events.AddDomainEvent(domainEvent);
    public bool RemoveDomainEvent(IDomainEvent domainEvent) => _events.RemoveDomainEvent(domainEvent);
    public void ClearDomainEvents() => _events.ClearDomainEvents();
}
```

## Architecture Overview

```
Craft.Domain/
├── Abstractions/           # Interfaces and contracts
│   ├── IEntity.cs
│   ├── IHasId.cs
│   ├── IHasConcurrency.cs
│   ├── ISoftDelete.cs
│   ├── IHasTenant.cs
│   ├── IHasUser.cs
│   ├── IHasActive.cs
│   ├── IHasVersion.cs
│   ├── IModel.cs
│   ├── IDataObject.cs
│   ├── IAggregateRoot.cs
│   ├── IDomainEvent.cs
│   └── IHasDomainEvents.cs
├── Base/                   # Base classes
│   ├── BaseEntity.cs
│   ├── BaseModel.cs
│   ├── BaseDTO.cs
│   ├── BaseVm.cs
│   └── ValueObject.cs
├── Events/                 # Domain events
│   └── DomainEventBase.cs
├── Enums/                  # Domain enumerations
├── Exceptions/             # One public namespace: Craft.Domain.Exceptions
│   ├── CraftException.cs
│   ├── ExceptionInfo.cs
│   ├── CraftExceptionFactory.cs
│   ├── Domain/
│   ├── Security/
│   ├── Infrastructure/
│   └── Http/
├── Extensions/             # Extension methods
├── Helpers/                # Constants and helpers
└── Resources/              # Localization resources
```

## Core Concepts

### Entities

Entities have identity and lifecycle. Use `BaseEntity<TKey>` or `BaseEntity` (which uses the configured `KeyType`).

```csharp
// With default long key
public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}

// With custom key type
public class Document : BaseEntity<Guid>
{
    public string Title { get; set; } = string.Empty;
}
```

**Built-in features:**
- `Id` - Primary key; generation is configured by persistence
- `ConcurrencyStamp` - GUID-based optimistic concurrency
- `IsDeleted` - Soft-delete support
- `Equals()` / `GetHashCode()` - Identity-based equality with optional tenant/shard criteria via `AdditionalEqualityCheck`
- `IEquatable<BaseEntity<TKey>>` - Type-safe equality

### Value Objects

Value objects are immutable and compared by their component values.

```csharp
// Multi-component value object
public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }
    
    public Address(string street, string city, string postalCode)
    {
        Street = street;
        City = city;
        PostalCode = postalCode;
    }
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return PostalCode;
    }
}

// Single-value wrapper (with validation)
public sealed class Email : SingleValueObject<string>
{
    public Email(string value) : base(value)
    {
        if (!IsValidEmail(value))
            throw new ArgumentException("Invalid email format", nameof(value));
    }
    
    private static bool IsValidEmail(string value) 
        => Regex.IsMatch(value, DomainConstants.EmailRegExpr);
}

// Usage
Email email = new("user@example.com");
string value = email; // Implicit conversion to string
```

### Aggregate Roots

Aggregate roots define consistency boundaries. Only aggregate roots should be loaded via repositories.

```csharp
// Mark aggregate roots with the marker interface
public class Order : BaseEntity, IAggregateRoot
{
    // Child entities are accessed only through the aggregate root
    private readonly List<OrderLine> _lines = [];
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();
}

// Repository constraint pattern
public interface IRepository<T> where T : class, IAggregateRoot
{
    Task<T?> GetByIdAsync(long id);
    Task AddAsync(T aggregate);
}
```

### Domain Events

Domain events represent something significant that happened in your domain.

```csharp
// Define a domain event
public sealed class OrderPlacedEvent : DomainEventBase
{
    public long OrderId { get; }
    public decimal TotalAmount { get; }
    
    public OrderPlacedEvent(long orderId, decimal totalAmount)
    {
        OrderId = orderId;
        TotalAmount = totalAmount;
    }
}

// Raise events from aggregate roots
public class Order : BaseEntity, IAggregateRoot, IHasDomainEvents
{
    private readonly DomainEventCollection _events = new();
    
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _events.DomainEvents;
    
    public void Place()
    {
        // Business logic...
        _events.AddDomainEvent(new OrderPlacedEvent(Id, TotalAmount));
    }
    
    public void AddDomainEvent(IDomainEvent e) => _events.AddDomainEvent(e);
    public bool RemoveDomainEvent(IDomainEvent e) => _events.RemoveDomainEvent(e);
    public void ClearDomainEvents() => _events.ClearDomainEvents();
}
```

**Domain Event Properties:**
- `EventId` - Non-empty GUID, generated for new events or restored for persisted events
- `OccurredOnUtc` - Occurrence timestamp with `DateTimeKind.Utc`
- `EventType` - CLR type name by default; override for a stable external contract name
- `CorrelationId` - Optional correlation for tracing
- `CausationId` - Optional link to causing event

`DomainEventBase()` generates a new ID and UTC timestamp. The timestamp constructor requires `DateTimeKind.Utc`; local and unspecified timestamps now throw `ArgumentException`. Convert known local times explicitly before creating an event.

For persisted events, forward the original ID and UTC timestamp to `base(eventId, occurredOnUtc)`. Equality and hashing use only `EventId`, even across event types; restoring an event must retain that ID. Do not generate a new ID during deserialization.

A concrete event can use a `System.Text.Json` constructor without a custom converter:

```csharp
public sealed class OrderPlacedEvent : DomainEventBase
{
    public long OrderId { get; }
    public decimal TotalAmount { get; }
    public override string EventType => "order-placed.v1";

    public OrderPlacedEvent(long orderId, decimal totalAmount)
    {
        OrderId = orderId;
        TotalAmount = totalAmount;
    }

    [System.Text.Json.Serialization.JsonConstructor]
    public OrderPlacedEvent(long orderId, decimal totalAmount,
        Guid eventId, DateTime occurredOnUtc) : base(eventId, occurredOnUtc)
    {
        OrderId = orderId;
        TotalAmount = totalAmount;
    }
}
```

This supports deserialization to the concrete event type. Polymorphic deserialization through `IDomainEvent` still needs a type-discriminator configuration in the consuming application. Keep payloads immutable as well; the base only supplies metadata.

`DomainEventCollection` preserves insertion order and allows duplicates. Its cached read-only view reflects subsequent additions, removals, and clearing; it is not a snapshot. Removal deletes the first equal event. The collection is intended for single-threaded entity use and does not dispatch or persist events.

### Data Transfer Objects

Three base classes for API communication, all implementing `IDataObject`:

| Class | Purpose | Use Case |
|-------|---------|----------|
| `BaseDTO` | API Input | Create/Update requests from client |
| `BaseVm` | API Output | Responses to client |
| `BaseModel` | General | Internal data transfer |

```csharp
// Input DTO for creating/updating
public class ProductDto : BaseDTO
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

// Output VM with computed properties
public class ProductVm : BaseVm
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string FormattedPrice => Price.ToString("C");
}
```

**Why include `ConcurrencyStamp` and `IsDeleted`?**
- `ConcurrencyStamp` - Client receives with response, sends back on updates for conflict detection
- `IsDeleted` - Enables soft-delete/restore operations via API

## Contracts (Interfaces)

| Interface | Purpose |
|-----------|---------|
| `IEntity<TKey>` | Entities with identity |
| `IHasId<TKey>` | Objects with an identifier |
| `IHasConcurrency` | Optimistic concurrency control |
| `ISoftDelete` | Soft-delete capability |
| `IHasTenant<TKey>` | Multi-tenant entities |
| `IHasUser<TKey>` | User-associated entities |
| `IHasActive` | Activation/deactivation |
| `IHasVersion` | Version tracking |
| `IModel<TKey>` | Data transfer models |
| `IDataObject<TKey>` | API transfer objects |
| `IAggregateRoot<TKey>` | DDD aggregate roots |
| `IDomainEvent` | Domain events |
| `IHasDomainEvents` | Event-raising entities |

## Exception Handling

Import `Craft.Domain.Exceptions` for all exception types. Folders group domain, security,
HTTP, and infrastructure responsibilities without requiring separate imports.

Constructors accept an optional message, cause, and enumerable of errors. Status codes
are fixed by type; error collections are immutable snapshots. `AlreadyExistsException`
and `ConcurrencyException` derive from `ConflictException` (409), `InvalidCredentialsException`
from `UnauthorizedException` (401), and `ExternalServiceException` from `BadGatewayException` (502).
`ModelValidationException` uses 400 and deep-copies property errors. `UnprocessableEntityException`
represents 422. `HttpStatusException` preserves other error statuses in the 400–599 range.

```csharp
using Craft.Domain.Exceptions;

throw new NotFoundException("Product", productId);
throw new DatabaseException("Save failed", innerException: cause, errors: ["Operation failed"]);
throw new ModelValidationException(new Dictionary<string, string[]>
{
    ["Name"] = ["Name is required"]
});
```

`ToErrorInfo()` creates JSON-serializable response data with the original status. It omits
exception types, stack traces, and inner exceptions, and replaces 5xx messages with a generic
message while omitting their errors. Client error messages and validation details must be
safe for your audience. `TooManyRequestsException(int retryAfterSeconds)` exposes `RetryAfter`
as a `TimeSpan`, also included in response data; HTTP adapters must set the Retry-After header
explicitly. Timestamped `GoneException` requires UTC deletion time.

```csharp
catch (CraftException exception)
{
    return Results.Json(exception.ToErrorInfo(), statusCode: exception.StatusCodeValue);
}
```

Use `ToErrorInfo(includeDetails: true)` only for trusted diagnostic destinations; it includes
raw messages and stack traces and follows up to 16 exceptions, including standard .NET causes.
Log the original exception through your application's Serilog provider when full diagnostics
are required. The library does not log automatically.

`CraftExceptionFactory.FromStatusCode(status, message, errors, innerException)` converts an
HTTP error without losing its status, details, or cause. `FromException(exception)` returns
existing Craft exceptions and cancellation exceptions unchanged. Access denial, timeout,
and unimplemented operations map to 403, 504, and 501; other runtime faults become 500 with
the original cause. It does not infer client fault from argument or invalid-operation exceptions.
The return type is `Exception` because cancellation retains its .NET semantics.

Breaking changes: replace the former category namespaces with `Craft.Domain.Exceptions`,
pass errors by name (`errors:`), and use constructors instead of forwarding factory methods.
Custom status overrides on named exception types were removed. `AlreadyExistsException`
consistently uses 409, and diagnostic export now requires `includeDetails: true`.

For expected business failures, prefer the consuming application's result abstraction over
throwing exceptions. A future separate HTTP/infrastructure error library would decouple
transport concerns from pure domain models; this review keeps package boundaries unchanged.

## Localization

Error messages support localization via resource files.

### Using Constants in Attributes (compile-time)

```csharp
[Required(ErrorMessage = DomainConstants.RequiredError)]
[StringLength(100, ErrorMessage = DomainConstants.MaxLengthError)]
public string Name { get; set; }
```

### Using Localized Messages at Runtime

```csharp
// Access localized messages
var message = DomainConstants.Localized.RequiredError;

// Use formatting helpers
var formatted = DomainConstants.Localized.FormatRequired("FirstName");
// Result: "FirstName is required"

// Or use DomainResources directly
var message = DomainResources.FormatEntityNotFound("Product", 42);
// Result: "Entity "Product" (42) was not found."
```

### Adding Translations

Create satellite resource files:
- `DomainResources.fr.resx` - French
- `DomainResources.de.resx` - German
- etc.

## Best Practices

### 1. Entity Design
- Keep entities focused on domain logic
- Use value objects for complex properties
- Implement `IHasDomainEvents` only on aggregate roots

### 2. Aggregate Root Rules
- External code should only reference aggregate roots
- All changes must go through the aggregate root
- Keep aggregates small for better concurrency

### 3. Value Objects
- Make them immutable (use `init` or constructor-only setters)
- Implement validation in constructor
- Override `GetEqualityComponents()` for all relevant properties

### 4. Domain Events
- Name events in past tense (OrderPlaced, UserRegistered)
- Include all relevant data in the event
- Events are immutable facts

### 5. DTOs
- Use `BaseDTO` for input (create/update requests)
- Use `BaseVm` for output (API responses)
- Don't expose domain entities directly via API

## Related Projects

- **Craft.Core** - `ServiceResult<T>` and core abstractions
- **Craft.Auditing** - Audit trail tracking
- **Craft.Data** - Entity Framework Core integration
- **Craft.Repositories** - Generic repository pattern

## License

MIT License - see [LICENSE](LICENSE) for details.

## Author

**Sandeep SHARMA**

---

*Built with ❤️ for .NET 10*
