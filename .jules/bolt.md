## 2026-01-17 - Global Search N+1 Fetching
**Learning:** The global search feature in `dashboard.js` triggers a full dataset fetch (`/api/learner/list-all`) on every keystroke > 1 char. This causes significant network overhead and server load (N requests for a search term of length N+1).
**Action:** Always verify "search on type" implementations for debounce and caching patterns. Implementing a 300ms debounce and 60s cache will reduce requests from N to ~1 per search session.

## 2026-01-18 - Preserving API Contracts in Micro-optimizations
**Learning:** Optimizing `StaffController` by adding `IsDBNull` checks and `ToString()` conversions changed the return type from `object` (nullable) to `string` (non-null). This broke the API contract, as consumers might expect `null` or specific types (like `int`).
**Action:** When implementing `GetOrdinal` optimizations, stick to `reader.GetValue(ord)` if the goal is purely performance, unless explicitly tasked with improving null safety or type consistency. Avoid "improvements" that alter the data shape during performance refactoring.

## 2026-01-22 - DBNull and Null-Coalescing Pitfalls
**Learning:** `DBNull.Value.ToString()` returns an empty string `""`, not `null`. Using `reader["Col"]?.ToString() ?? "Default"` is ineffective for trapping `DBNull` values because the left side evaluates to `""`, bypassing the null-coalescing operator.
**Action:** When optimizing, replicate the exact behavior (even if seemingly buggy) unless fixing the bug is explicitly requested. Use `reader[ordinal]` syntax instead of `reader.GetValue(ordinal)` for cleaner code that preserves this specific behavior.
