# Copilot Memory

> **Purpose:** Persistent cross-session knowledge base. The AI reads this file at session start and appends learnings during work.
> **Rule:** Only add entries that would save time or prevent mistakes in future sessions. Keep entries concise (1–2 lines each).

---

## Corrections

<!-- User corrections to AI behavior or output. Format: `- YYYY-MM-DD: <what was wrong> → <what is correct>` -->

## Preferences

<!-- Explicit user preferences for code style, tooling, or workflow beyond what .editorconfig covers. -->
- 2026-09-09: Prefer the newer C# extension member syntax (`extension<T>(...)`) for extension methods when supported by the language and applicable to the target type.

## Architecture Decisions

<!-- Non-obvious technical decisions made during development. Include brief rationale. -->
- 2026-09-08: Collection-related extensions are split by target type (`ICollection<T>`, `IEnumerable<T>`, membership-on-item) and queryable extensions live in the LINQ area to keep files cohesive and discoverable.

## Patterns & Conventions

<!-- Recurring patterns discovered in the codebase that new code should follow. -->

## Gotchas

<!-- Pitfalls, quirks, or non-obvious behaviors encountered in this workspace. -->
