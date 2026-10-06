# Craft_V_11

## Build before pushing

Install the repository's Git hooks once in each clone:

```sh
git config --local core.hooksPath .githooks
```

After that, `git push` builds the full solution in Release configuration and stops if the build fails or the .NET SDK is missing. Run `dotnet build Craft_V_11.slnx --configuration Release` at any time to check the build before pushing.

Git hooks are local to each clone and can be bypassed with `git push --no-verify`. The existing CI workflow provides an additional check for Craft.Extensions.

## Default key type

`Directory.Build.props` defines `CraftDefaultKeyType` once for all SDK-style C# projects in this repository. It defaults to `System.Int64` (`long`) and generates the global `KeyType` alias; do not declare that alias again in project source files.

Non-generic contracts and bases use the configured type, for example `IModel : IModel<KeyType>` and `BaseEntity : BaseEntity<KeyType>`. Generic types such as `BaseEntity<Guid>` remain independent of the default.

To change the source-wide default, edit `CraftDefaultKeyType` in `Directory.Build.props`, then rebuild affected libraries and consumers. A temporary override is also supported:

```sh
dotnet build Source/Core/Craft.Domain/Craft.Domain.csproj --configuration Release -p:CraftDefaultKeyType=System.Guid
```

Use the same override for every build/test invocation in that configuration. Projects with a nearer `Directory.Build.props` must explicitly import the repository configuration. External projects do not receive this alias through a project or package reference; import equivalent shared build configuration if their own source uses `KeyType`.

Changing the default changes compiled public signatures. It cannot change an already-built NuGet package. Numeric test data and application code may need updating when switching to `Guid`; database keys, foreign keys, migrations and ID generation need a separate migration plan. The default `long` supports numeric database identity generation; a GUID requires an appropriate GUID generation strategy. .NET's `System.Guid` represents UUID values.

To use another key type without rebuilding Craft, use its generic contracts and bases instead. See [Craft.Domain key configuration](Source/Core/Craft.Domain/README.md#default-key-type).
