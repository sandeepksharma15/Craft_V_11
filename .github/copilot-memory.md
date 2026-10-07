# Copilot Memory

> **Purpose:** Persistent cross-session knowledge base. The AI reads this file at session start and appends learnings during work.
> **Rule:** Only add entries that would save time or prevent mistakes in future sessions. Keep entries concise (1–2 lines each).

---

## Corrections

<!-- User corrections to AI behavior or output. Format: `- YYYY-MM-DD: <what was wrong> → <what is correct>` -->

## Preferences

<!-- Explicit user preferences for code style, tooling, or workflow beyond what .editorconfig covers. -->
- 2026-09-09: Prefer the newer C# extension member syntax (`extension<T>(...)`) for extension methods when supported by the language and applicable to the target type.
- 2026-09-11: In test projects, prefer reusing existing shared fixture classes (for example `TestExpressionModels`) over creating new private test classes; keep nomenclature consistent across projects.

## Architecture Decisions

<!-- Non-obvious technical decisions made during development. Include brief rationale. -->
- 2026-09-08: Collection-related extensions are split by target type (`ICollection<T>`, `IEnumerable<T>`, membership-on-item) and queryable extensions live in the LINQ area to keep files cohesive and discoverable.
- 2026-09-29: Keep reusable test models and EF Core fixtures in `Tests/Support/Craft.Testing`; keep fixtures requiring a Craft library with that library's test project.
- 2026-09-29: Group reflection test shapes under `Craft.Testing.Reflection.ReflectionTestModels` so multiple test suites can reuse inheritance, nested properties, clone graphs, and private-constructor cases.
- 2026-09-29: Expression extension APIs remain in Craft.Expressions but use `System.Linq.Expressions` to be available with the natural BCL namespace; keep the package's auto-using list aligned.

- 2026-09-30: Utilities helpers retain `Craft.Utilities.Helpers` while source/tests are grouped into Timing, IO, Text and Resilience folders; use injectable `TimeProvider` and fake time for scheduling tests.

- 2026-10-06: Define the source-wide default key type through CraftDefaultKeyType in root Directory.Build.props (System.Int64 by default); use its generated KeyType alias instead of per-project aliases. Generic key contracts remain independent.

- 2026-10-07: Domain transfer bases are mutable classes with reference equality. Entity equality requires an assigned ID and the same runtime type; configure key generation in persistence rather than a generic Identity annotation.

- 2026-10-07: DomainEventBase requires UTC timestamps and supports restoring a non-empty event ID; event equality remains ID-only. Concrete JSON event constructors must forward persisted metadata rather than regenerate it.

- 2026-10-07: Craft exceptions are grouped under Craft.Domain.Exceptions subnamespaces; named types have fixed statuses and immutable error snapshots. ToErrorInfo hides server diagnostics by default; FromException preserves cancellation and never infers client fault from runtime argument errors.

## Patterns & Conventions

<!-- Recurring patterns discovered in the codebase that new code should follow. -->

## Gotchas

<!-- Pitfalls, quirks, or non-obvious behaviors encountered in this workspace. -->

- 2026-09-30: Reflection property value APIs support case-sensitive dotted paths with public-only access by default and explicit non-public opt-in; use strict assignment and write back nested structs. Setters require reference-type roots and reject init-only properties.

- 2026-09-30: Generate simple DOCX/PDF test inputs during Arrange with Craft.Testing.Documents.TestDocumentFactory and isolated disposable Craft.Testing.IO.TemporaryDirectory; avoid copy-to-output binary fixtures for Live Testing portability.

- 2026-10-07: All runnable test projects use native Microsoft Testing Platform via global.json, xunit.v3 and coverlet.MTP; avoid VSTest packages and legacy TestingPlatformDotnetTestSupport. Coverage reports have timestamped filenames.

- 2026-10-07: DomainExtensions lives with its contracts in Craft.Domain.Abstractions. Null/default IDs never match tenant/user associations; IsNullOrDefault uses IHasId and exact default-key semantics (negative IDs are assigned). Parsing and creator-audit aliases do not belong in these extensions.
