# Craft agent instructions

Before writing or editing code:

1. Read [CODING_STYLE.md](CODING_STYLE.md) and `.editorconfig`.
2. Read [.github/copilot-instructions.md](.github/copilot-instructions.md) and the relevant project guidance.
3. Inspect the actual implementation, tests and configuration. Read `.github/copilot-memory.md` before substantive work.

Sandeep's confirmed readability preferences apply across production code, tests and examples:

- Explicit types by default; `var` is allowed when the type is immediately obvious.
- Standard member regions in larger classes: fields, constructors, properties and methods.
- Minimal essential comments, with brief XML documentation only where the public contract needs explanation.
- One statement per line and clear whitespace between logical steps.

Follow current user instructions when they refine these preferences. Keep changes focused, preserve the approved architecture and validate behavioral changes with relevant builds and tests. Do not undertake repository-wide reformatting unless requested.
