# Craft.CryptKey

Stable, reversible, URL-friendly IDs for .NET 11. Supports `int`, `long` (including negative values), and `Guid`. This is obfuscation, not encryption, tamper protection or authorization.

## Usage

Configure one encoder per application; there is no built-in salt or implicit default encoder:

```csharp
services.AddHashKeys().Configure(options =>
{
    options.Salt = configuration["HashKeys:Salt"]!;
    options.MinHashLength = 10;
    options.MaxHashLength = 256;
});
```

Inject `IHashKeys`, or construct `new HashKeys(options)` without DI:

```csharp
string token = 42L.ToHashKey(hashKeys);                 // int, long or Guid
long id = token.ToKeyType<long>(hashKeys);
KeyType defaultId = token.ToKeyType(hashKeys);         // configured build-time alias

if (token.TryToKeyType<long>(hashKeys, out var key))
{
    // Authorize access to the entity identified by key.
}
```

The service also exposes `Encode<TKey>`, `Decode<TKey>` and `TryDecode<TKey>`. `AddHashKeys(options => ...)` is available for inline configuration. Registration is idempotent, validates on host startup, and creates a singleton with a configuration snapshot.

## Contract

- Keep salt, alphabet, separators and minimum length stable wherever tokens are exchanged or persisted. Configure them externally; changing them can invalidate existing tokens. The salt is not an encryption key.
- `Alphabet` and `Separators` default to Hashids' alphabet/separators and can be customized. Validation requires unique URL-safe ASCII characters and valid Hashids settings. `Salt` is required and cannot have surrounding whitespace.
- `MaxHashLength` bounds decoding work and encoded output. Increase it if your minimum length or alphabet requires longer tokens; an oversized encoding throws `InvalidOperationException`.
- Invalid, null, empty, oversized, wrong-type or malformed tokens return `false`/default from `TryDecode` and `TryToKeyType`; strict decoding throws `FormatException`. Unsupported key types throw `NotSupportedException`.
- Numeric types have distinct tokens. Decode an `int` token as `int`, not `long`. GUIDs use a platform-independent byte order. Zero and `Guid.Empty` are valid keys; domain validation is the caller's responsibility.
- Use the default case-sensitive ordinal semantics; do not lowercase, trim or otherwise normalize tokens. Wrong configuration usually fails decoding but is not authenticated, and modified tokens can identify other keys. Always enforce authorization.

This typed format replaces the previous long-only format. Old tokens, parameterless conversions, `IServiceProvider` overloads, `Steps` (now `Separators`) and inherited Hashids APIs are not supported. Rebuild consumers and regenerate old tokens when migrating. Keep the pinned Hashids dependency and compatibility-vector tests aligned before changing encoding behavior.
