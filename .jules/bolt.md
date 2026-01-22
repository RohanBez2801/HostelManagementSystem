## 2026-01-17 - Global Search N+1 Fetching
**Learning:** The global search feature in `dashboard.js` triggers a full dataset fetch (`/api/learner/list-all`) on every keystroke > 1 char. This causes significant network overhead and server load (N requests for a search term of length N+1).
**Action:** Always verify "search on type" implementations for debounce and caching patterns. Implementing a 300ms debounce and 60s cache will reduce requests from N to ~1 per search session.

## 2026-01-24 - ADO.NET Column Lookup Overhead
**Learning:** Using `reader["ColumnName"]` inside loops (e.g., `GetAllLearners`) performs repeated hash lookups. In Access DB via OleDb, this adds measurable overhead for large datasets.
**Action:** Use `reader.GetOrdinal("ColumnName")` before the loop and `reader.GetInt32(ord)` or `reader[ord]` inside the loop. Use `reader.IsDBNull(ord)` for robust null checking.
