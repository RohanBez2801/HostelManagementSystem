## 2026-01-17 - Global Search N+1 Fetching
**Learning:** The global search feature in `dashboard.js` triggers a full dataset fetch (`/api/learner/list-all`) on every keystroke > 1 char. This causes significant network overhead and server load (N requests for a search term of length N+1).
**Action:** Always verify "search on type" implementations for debounce and caching patterns. Implementing a 300ms debounce and 60s cache will reduce requests from N to ~1 per search session.

## 2026-01-17 - OLEDB Column Lookup Bottleneck
**Learning:** In `LearnerController.cs`, `GetAllLearners` was using string-based column lookups inside a `while(reader.Read())` loop. For Access/OLEDB, this triggers a hash lookup for every column for every row (O(N*M) lookups).
**Action:** Implemented `reader.GetOrdinal()` pattern to cache indices outside the loop. Also discovered `Grade` and `RoomNumber` columns have inconsistent types (Text vs Int) in the Access DB, requiring `reader.GetValue(ord).ToString()` instead of strict typed getters to prevent crashes.
