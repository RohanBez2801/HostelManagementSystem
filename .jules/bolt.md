## 2026-01-17 - Global Search N+1 Fetching
**Learning:** The global search feature in `dashboard.js` triggers a full dataset fetch (`/api/learner/list-all`) on every keystroke > 1 char. This causes significant network overhead and server load (N requests for a search term of length N+1).
**Action:** Always verify "search on type" implementations for debounce and caching patterns. Implementing a 300ms debounce and 60s cache will reduce requests from N to ~1 per search session.

## 2026-01-17 - DBNull.ToString() Behavior in C#
**Learning:** `DBNull.Value.ToString()` returns an empty string `""`. Consequently, constructs like `reader["Col"]?.ToString() ?? "Default"` evaluate to `""` for DBNulls, not `"Default"`. This can subtly break logic where a non-empty default is expected for missing values.
**Action:** Always use explicit `reader.IsDBNull(ord) ? "Default" : ...` checks when a specific default value is required for database nulls.
