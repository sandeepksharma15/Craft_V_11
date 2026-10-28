# Craft.Extensions

Craft.Extensions is a .NET 11 utility library built around concise, discoverable extension APIs for common .NET types and application infrastructure.

## Features

- Collection and enumerable helpers
- LINQ and Entity Framework Core query helpers
- Date/time, enum, numeric, string, reflection, and type helpers
- Dependency injection helpers
- HTTP response helpers
- File-provider and image-file helpers
- Hex and byte-size conversion helpers

The library uses modern C# extension blocks and targets .NET 11.

## Usage

```csharp
var list = new List<int> { 1, 2 };
list.AddIfNotContains(3);

var today = DateTime.Now.ClearTime();
var hex = new byte[] { 0x01, 0xAB }.BytesToHex();
```

## Type contract checks

`type.HasImplementedInterface(interfaceType)` accepts ordinary interfaces, closed generic interfaces, and open generic interface definitions such as `IEnumerable<>`. It includes the interface itself and inherited implementations; closed interfaces preserve normal assignability and variance. Null inputs and non-interface targets return false.

`type.IsGenericTypeOf(typeof(List<>))` checks only the type's own generic definition, not its base classes or interfaces. A null receiver returns false. The definition argument must be a non-null generic type definition; invalid arguments throw `ArgumentNullException` or `ArgumentException`.

## License

MIT. See `LICENSE`.
