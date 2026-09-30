# Craft.Utilities

Craft.Utilities is a .NET 10 library providing a rich set of utility classes, helpers, and services to accelerate development and promote code reuse in C# applications. It includes utilities for caching, password generation, file uploads, text processing, timing, and more.

## Features
- **Caching Services**: In-memory and Redis cache implementations with a common interface.
- **Password Generation**: Cryptographically random password generator with configurable length.
- **File Upload Service**: Abstraction for file uploads and storage.
- **Helpers**: Utilities for random value generation, debouncing, countdown timers, parameter replacement in expressions, and more.
- **Builders**: Fluent builders for CSS, style, and value construction.
- **Managers**: Observer pattern manager for event-driven scenarios.
- **Validators**: URL validation and other input helpers.
- **Text Utilities**: Text extraction and conversion helpers.
- **Extensible & Testable**: Designed for easy extension and unit testing.
- **.NET 10 & C# 13**: Utilizes the latest language and framework features.

## Getting Started

### Installation
Add a project reference to `Craft.Utilities` in your .NET 10 solution:

```
dotnet add reference ../Craft.Utilities/Craft.Utilities.csproj
```

### Usage Example
```csharp
using Craft.Utilities;
using Craft.Utilities.CacheService;
using Craft.Utilities.Passwords;

// Use the in-memory cache service
ICacheService cache = new MemoryCacheService();
cache.Set("key", "value");
var value = cache.Get<string>("key");

// Generate a secure password
var passwordService = new PasswordGeneratorService();
string password = passwordService.GeneratePassword(16);
```

The password APIs require at least 6 characters and default to 8 for compatibility. Each generated password contains at least one uppercase letter, lowercase letter, digit, and character from `!@#$%^&*()_+[]{}|;:,.<>?`. Pass the desired length explicitly, as in the 16-character example above.

Register the default singleton with `services.AddPasswordGeneratorService()` and consume `IPasswordGeneratorService`. Repeated registration is safe and preserves an existing custom implementation.

## Key Components
- `ICacheService`, `MemoryCacheService`, `RedisCacheService`: Caching abstractions and implementations.
- `IPasswordGeneratorService`, `PasswordGeneratorService`: Password generation utilities.
- `FileUploadService`: File upload abstraction.
- `RandomHelper`, `Debouncer`, `CountdownTimer`: General-purpose helpers.
- `CssBuilder`, `StyleBuilder`, `ValueBuilder`: Fluent builders for UI and data scenarios.
- `ObserverManager`: Event observer pattern manager.
- `UrlValidatior`: URL validation helper.
- `TextExtractor`, `TextConverters`: Text processing utilities.

## Integration
Craft.Utilities is designed to be referenced by .NET projects that require reusable, high-quality utility functions and helpers for common development scenarios.

## License
This project is licensed under the MIT License. See the `LICENSE` file for details.

---
For more details, review the source code and XML documentation in the project.

