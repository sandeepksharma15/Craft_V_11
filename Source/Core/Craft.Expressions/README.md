# Craft.Expressions

`Craft.Expressions` helps you convert predicate expressions between strings and LINQ expression trees, then compose and reuse them in application and EF Core query scenarios.

## Installation

```bash
dotnet add package Craft.Expressions
```

## Automatic namespace imports

When referenced as a package, `Craft.Expressions` adds transitive global usings for:

- `Craft.Expressions.Engine`
- `Craft.Expressions.Extensions`
- `Craft.Expressions.Linq`
- `Craft.Expressions.EntityFramework`
- `Craft.Expressions.Comparison`

To opt out in a consuming project:

```xml
<PropertyGroup>
  <CraftExpressionsDisableAutoUsings>true</CraftExpressionsDisableAutoUsings>
</PropertyGroup>
```

Or disable auto-usings across Craft packages:

```xml
<PropertyGroup>
  <CraftDisableAutoUsings>true</CraftDisableAutoUsings>
</PropertyGroup>
```

## Core functionality

- Deserialize filter text into `Expression<Func<T, bool>>`
- Serialize expression trees back to stable text
- Compose predicates (`And`, `Or`) and edit conditions (`RemoveCondition`, `ReplaceCondition`)
- Compare expressions semantically
- Build member-access expressions and extract property paths
- Work with EF Core query filters (`GetQueryFilter`, `RemoveFromQueryFilter`, `ApplyQueryFilter`)

## Usage

### 1) Parse a filter string

```csharp
using Craft.Expressions.Engine;

var serializer = new ExpressionSerializer<Person>();
var predicate = serializer.Deserialize("Age >= 18 && IsActive == true");

bool match = predicate.Compile()(new Person { Age = 21, IsActive = true });
```

### 2) Serialize an expression

```csharp
using Craft.Expressions.Engine;

var serializer = new ExpressionSerializer<Person>();
Expression<Func<Person, bool>> predicate = p => p.Age >= 18 && p.IsActive;

string text = serializer.Serialize(predicate);
// Example: "(Age >= 18) && IsActive"
```

### 3) Round-trip stored filters

```csharp
using Craft.Expressions.Engine;

var serializer = new ExpressionSerializer<Person>();
string stored = serializer.Serialize(p => p.Name.Contains("smith"));
Expression<Func<Person, bool>> restored = serializer.Deserialize(stored);
```

### 4) Compose predicates with `And` / `Or`

```csharp
using Craft.Expressions.Extensions;

Expression<Func<Person, bool>> adults = p => p.Age >= 18;
Expression<Func<Person, bool>> active = p => p.IsActive;

var activeAdults = adults.And(active);
var adultsOrActive = adults.Or(active);
```

### 5) Remove or replace conditions in a predicate

```csharp
using Craft.Expressions.Extensions;

Expression<Func<Person, bool>> filter = p => p.IsActive && p.Age >= 18;

var withoutActive = filter.RemoveCondition(p => p.IsActive);
var stricterAge = filter.ReplaceCondition(p => p.Age >= 18, p => p.Age >= 21);
```

### 6) Compare expression meaning (semantic equality)

```csharp
using Craft.Expressions.Extensions;

Expression<Func<Person, bool>> a = x => x.IsActive && x.Age >= 18;
Expression<Func<Person, bool>> b = p => p.Age >= 18 && p.IsActive;

bool same = a.Body.IsSemanticallyEquivalentTo(b.Body); // true
```

### 7) Create member access expressions and extract property paths

```csharp
using Craft.Expressions.Extensions;

var selector = "Name".CreateMemberExpression<Person, string>();
string path = ((Expression<Func<Person, object>>)(p => p.Address.City)).GetFullPropertyPath();
// selector: p => p.Name
// path: "Address.City"
```

### 8) Use EF Core query filter helpers

```csharp
using Craft.Expressions.EntityFramework;
using Craft.Expressions.Linq;

// Read configured global query filter for entity
var filter = dbContext.Set<Person>().GetQueryFilter();

// Apply DbSet filter to another queryable
var visiblePeople = dbContext.People.AsQueryable().ApplyQueryFilter(dbContext.People);

// Remove one condition from the global filter for this query
var withDeletedIncluded = dbContext.People.RemoveFromQueryFilter(p => p.IsDeleted == false);
```

## Supported syntax in filter strings

- Logical: `&&`, `||`, `!`
- Comparison: `==`, `!=`, `>`, `>=`, `<`, `<=`
- Grouping: `(` `)`
- Values: strings (`"abc"`), numbers, booleans, `null`
- Member access: `Address.City`
- Instance method calls: `Name.Contains("abc")`, `Name.StartsWith("A")`

## Exceptions

- `ExpressionTokenizationException` for invalid tokens/syntax
- `ExpressionParseException` for invalid token structure
- `ExpressionEvaluationException` when members/methods cannot be resolved
- `ArgumentException` for empty input or strings beyond `ExpressionSerializer<T>.MaxExpressionLength`

## License

MIT
