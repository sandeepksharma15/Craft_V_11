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

## License

MIT. See `LICENSE`.
