# Sandeep's Craft coding style

Applies to code written or edited by ChatGPT, Codex, Copilot and other contributors in Craft. Derived from representative Craft_V_10 source and `.editorconfig`, with Sandeep's explicit preferences confirmed on 18 November 2026. Use this document for readability choices and `.editorconfig` for mechanical formatting.

## Core principle

Write straightforward code that is easy to scan, debug and maintain. Keep each logical step visible. Concise code means little unnecessary machinery, not fewer physical lines. Apply this style to production code, tests, examples and project files.

Inspect the surrounding implementation before editing. Preserve the current V11 architecture and behavior unless the task authorizes a change. V10 is a style reference; its historical dependencies, exception handling and persistence behavior are not architectural requirements.

## Layout

- Use four spaces, file-scoped namespaces and using directives outside the namespace. Follow `.editorconfig` for ordering, whitespace and line endings.
- Use Allman braces for type declarations, block-bodied members and control-flow blocks.
- Put each statement on its own line. Put each property or field declaration on its own line. Do not compress methods, test setup, assertions or several declarations into a single line.
- Keep simple auto-properties and genuinely trivial expression-bodied properties compact.
- Separate members with a blank line. Within a method, use blank lines between validation, preparation, execution and result handling where those stages exist.
- A short guard or other unambiguous single-statement branch may omit braces. Put its statement on the next line. Use braces for multiple statements or when they improve clarity.
- Wrap long signatures, base lists, generic constraints and calls at logical boundaries. Keep continuation indentation consistent; put separate generic constraints on separate lines when wrapping.
- Expand multi-member object initializers and long fluent chains over several lines. Do not pack project-file property groups and item groups onto one line.

## Types and naming

- **Explicit local types are the default.** Allow `var` when the type is immediately obvious from the initializer, such as `new Dictionary<int, string>()`, or when required for an anonymous type. Do not assume a method's return type is obvious just from its name.
- Prefer explicit types for awaited results, service calls and other expressions whose types require looking elsewhere. For example, use `Customer? customer = await repository.GetAsync(id, cancellationToken: token);`.
- Target-typed `new()` is appropriate when the declared type is visible. Avoid repeating long type names without benefit.
- Use PascalCase for types and members, camelCase for parameters and locals, and `_camelCase` for private/protected backing fields and dependency fields. Prefix interfaces with `I`.
- Use meaningful names. Prefer `repository`, `customer`, `options` and `cancellationToken` over unexplained abbreviations in ordinary application code. Conventional short lambda or loop names are acceptable in small, obvious expressions.
- Use explicit accessibility and `readonly` for fields that do not change. Use `TKey`, `TEntity`, `TResult` and similarly descriptive generic names when several roles need distinction; `T` remains suitable for a simple single-type utility.

The existing explicit-type analyzer preferences are defaults. The confirmed allowance for obvious `var` initializers is a discretionary exception, not a request to convert every obvious declaration to `var`.

## Class and file organization

- Keep a file focused on one principal responsibility. Closely related generic/default-key variants may share a file, following the surrounding project.
- **Larger classes use standard member regions**, in this order: `Fields`, `Constructors`, `Properties`, `Methods`. Add other meaningful groups only when needed, such as `Events` or `Nested Types`.
- Omit empty regions. Small classes and interfaces do not need a region around every member. Avoid nested regions and regions inside method bodies.
- Keep overloads and related methods together. Within the methods section, present the public workflow clearly and keep implementation helpers together. Follow an established local public/protected/private grouping where one exists.
- Primary constructors are welcome for simple dependency forwarding. Use a conventional constructor when validation or initialization benefits from an explicit sequence. Do not force either form everywhere.
- Keep dependencies explicit. Create abstractions and helper classes only when they clarify responsibilities or remove meaningful duplication.

## Methods and expressions

- Prefer early argument validation and guard returns. Use standard guard APIs where appropriate.
- Use block bodies for multi-step work. Give intermediate values names when that helps explain the operation.
- A short forwarding method or simple calculation may use an expression body. For methods, the familiar layout is the signature followed by an indented `=>` on the next line.
- Use modern C# features—pattern matching, collection expressions, records and extension members—when they improve readability. Use loops for multi-step mutation and LINQ for clear transformations and queries.
- Avoid deeply nested ternaries, dense condition chains and unnecessary wrapping methods. Extract a named predicate only when it explains a meaningful concept.
- Keep asynchronous I/O asynchronous and propagate cancellation. Add `async`/`await` when needed for actual asynchronous work, lifetime management or exception flow. Do not introduce task wrappers solely for stylistic symmetry.
- Keep structured logging compatible with the host's Serilog provider, and follow existing error and persistence contracts.

## Comments and documentation

- **Keep comments minimal and essential.** Explain a non-obvious reason, invariant, ownership rule or limitation.
- Add brief XML documentation where the public contract needs explanation: side effects, transaction scope, cancellation, null semantics, lifetime requirements or meaningful constraints.
- Omit comments that simply restate a member name or narrate an assignment, branch or return.
- Put extended setup instructions, examples and architectural rationale in the README or other documentation. Record deferred work in TODO rather than leaving speculative inline commentary.

## Tests

- Use xUnit and existing project conventions. Prefer behavior-oriented names such as `Method_Scenario_ExpectedBehavior`.
- Keep Arrange, Act and Assert visually separated with blank lines. Add section comments only when they help a longer test.
- Give setup, operations and assertions separate lines. Use descriptive names and the same type and formatting rules as production code.
- Prefer focused tests and parameterized cases for genuinely parallel scenarios. Keep supporting types readable; do not compress entire fixtures into one-line class bodies.
- Validate behavioral changes with relevant builds/tests and required coverage gates. For documentation-only changes, check links, consistency and the diff; do not claim tests were run when they were not.

## Layout example

This example illustrates formatting and organization, not a required architecture. Regions are shown together for reference; a class this small can omit them.

```csharp
namespace Craft.Examples;

public sealed class CustomerLookup
{
    #region Fields

    private readonly IReadOnlyDictionary<int, string> _names;

    #endregion

    #region Constructors

    public CustomerLookup(IReadOnlyDictionary<int, string> names)
    {
        ArgumentNullException.ThrowIfNull(names);

        _names = names;
    }

    #endregion

    #region Properties

    public int Count => _names.Count;

    #endregion

    #region Methods

    public string? GetName(int id)
    {
        if (!_names.TryGetValue(id, out string? name))
            return null;

        return name;
    }

    public bool Contains(int id)
        => _names.ContainsKey(id);

    #endregion
}
```

## Copyable ChatGPT project instructions

> Follow Sandeep's Craft coding style. Read CODING_STYLE.md and .editorconfig before writing code, and inspect the existing implementation first. Prefer explicit local types; allow var when the type is immediately obvious or required for an anonymous type. Use four-space indentation, file-scoped namespaces, Allman block braces, descriptive names and _camelCase fields. Put statements, declarations, test steps and assertions on separate lines. Separate logical stages with blank lines. Use early guards, straightforward control flow and short expression bodies for simple forwarding methods. For larger classes, organize members into Fields, Constructors, Properties and Methods regions, omitting empty groups. Use primary constructors and modern C# features where they improve clarity. Keep comments minimal; add brief XML documentation only where the public contract needs explanation. Keep tests and project files as readable as production code. Preserve the approved V11 architecture and prefer the smallest complete solution. Reformat the code you touch as needed, without unrelated repository-wide cleanup. Build and run relevant tests after behavioral changes, and report validation accurately.

## Before completing a code change

Check that the result reads like the surrounding hand-written Craft code: clear member organization, visible logical steps, intentional names and no compressed statement sequences. Review the diff for accidental behavior changes and unrelated formatting. Apply this guideline to new and modified code; repository-wide reformatting is a separate task.
