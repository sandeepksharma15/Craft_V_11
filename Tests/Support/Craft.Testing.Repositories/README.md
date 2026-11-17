# Craft.Testing.Repositories

Test-only repository stubs, separate from production code and the general Craft.Testing utilities. Derive from `RepositoryStub<TEntity, TKey>` (or its default-key variant) and override only operations configured by your test. Every unconfigured operation throws `NotSupportedException`; there are no silent mutation successes or fake commits.

Use these for application orchestration tests. Use real relational repositories for query translation, tracking, filtering, transactions, auditing and concurrency. The stub deliberately does not pretend to emulate EF.
