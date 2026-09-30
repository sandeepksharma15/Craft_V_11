# Helpers

All public types retain the `Craft.Utilities.Helpers` namespace. Source and test folders group
Timing (`Debouncer`, `CountdownTimer`), IO (`FileHelper`), Text (`TextConverters`, `TextExtractor`,
`TextSimilarity`), and Resilience (`RetryHelper`). Random selection remains a small standalone helper.
These are implementation folders, not additional assemblies or namespaces.

## Contracts

- **Timing:** Both types accept a `TimeProvider` for controlled scheduling. Debounce is trailing-edge;
  throttle schedules the latest pending action at a rate measured between action starts using monotonic
  time. Debounce and throttle share one pending slot. Disposal prevents pending work from starting,
  but cannot interrupt an already started callback. Callbacks execute outside the state lock.
  Subscribe to `OnError` to observe callback failures; error handlers must not throw.
  Countdown Stop pauses the tick count; Start resumes it, while Reset clears it. A completed countdown
  requires Reset before another Start. Tick intervals must be at least 1 ms and fit the system timer range.
  One-shot scheduling prevents overlapping countdown handlers; handler execution can extend total duration.
- **Retries:** Existing overloads and final `InvalidOperationException` wrapping are retained.
  Cancellation is always propagated immediately, including exception-specific and backoff retries.
  Token-aware async overloads pass the caller's token to the operation as well as delays.
  Backoff is capped without floating-point conversion or overflow. Broad retry overloads retry all
  non-cancellation exceptions; choose exception-specific overloads for known transient failures.
- **Files:** Unique names are availability checks, not reservations. Only leaf file names are accepted.
  Async copy uses atomic `CreateNew` when overwrite is false. Pre-cancelled copy/delete operations
  do not modify files. A copy cancelled after writing starts may leave a partial destination.
  Delete returns false when missing, denied, or exhausted; retry parameters must be nonnegative.
  File-size labels use powers of 1024 and invariant formatting through EB.
  Sanitization uses the current operating system's invalid characters and is not cross-platform naming validation.
  File comparisons fill buffers to handle partial reads correctly; concurrent modification is not a snapshot guarantee.
- **Text extraction:** PDF and DOCX are supported. Legacy binary DOC is explicitly rejected.
  Malformed supported documents return empty for compatibility; file access failures propagate.
  Missing Word body text returns empty. PDF text extraction is not OCR.
- **Markdown to RTF:** CommonMark headings, paragraphs, emphasis, lists, links, quotes and code are
  rendered directly from the syntax tree. Text is escaped for RTF and Unicode is encoded as signed UTF-16
  control words. Raw HTML is literal text; images become alternative text. This is a basic converter,
  not a full document-layout or HTML-rendering engine.
- **Random:** Uses `Random.Shared` for ordinary selection and linear-time shuffling, preserving all
  occurrences without changing the input. It is not for credentials; use the password namespace there.
- **Similarity:** Levenshtein distance is case-sensitive, treats null as empty, counts UTF-16 code units,
  and uses working memory proportional to the shorter string. It does not normalize Unicode or compare graphemes.

## Potential additions

Add these when a consumer needs them, rather than growing a miscellaneous helper collection preemptively:

- Retry predicates and jittered backoff, plus non-value exception-specific overloads. Larger resilience
  policies may justify a dedicated library.
- Explicit leading/trailing throttle options and cancellation-token-aware scheduled actions.
- A bounded edit-distance API and normalized similarity score, with an explicit Unicode/case policy.
- Stream-based extraction and conversion APIs; legacy DOC or OCR belong behind optional integrations.
- Atomic file creation/reservation as a separate API when callers need more than an available name.

Tests use fake time and controlled queued callbacks, deterministic random invariants, real temporary files,
and actual DOCX/PDF documents. CI runs the complete solution and reports reviewed utility coverage on
Linux and Windows, including OS-specific file locking and permissions.
