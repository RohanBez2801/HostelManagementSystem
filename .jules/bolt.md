## 2026-01-17 - Global Search N+1 Fetching
**Learning:** The global search feature in `dashboard.js` triggers a full dataset fetch (`/api/learner/list-all`) on every keystroke > 1 char. This causes significant network overhead and server load (N requests for a search term of length N+1).
**Action:** Always verify "search on type" implementations for debounce and caching patterns. Implementing a 300ms debounce and 60s cache will reduce requests from N to ~1 per search session.

## 2024-05-22 - ADO.NET IDataReader Performance
**Learning:** Using string-based column lookups (e.g., `reader["Column"]`) inside a `while(reader.Read())` loop forces the ADO.NET driver to perform a hash map lookup for every single row, which is inefficient for large datasets.
**Action:** Always cache column ordinals using `reader.GetOrdinal("Column")` *outside* the loop and use the integer indices inside the loop for O(1) access.
