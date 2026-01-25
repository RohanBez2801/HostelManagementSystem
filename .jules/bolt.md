## 2026-01-17 - Global Search N+1 Fetching
**Learning:** The global search feature in `dashboard.js` triggers a full dataset fetch (`/api/learner/list-all`) on every keystroke > 1 char. This causes significant network overhead and server load (N requests for a search term of length N+1).
**Action:** Always verify "search on type" implementations for debounce and caching patterns. Implementing a 300ms debounce and 60s cache will reduce requests from N to ~1 per search session.

## 2026-01-24 - Scope Creep in Performance PRs
**Learning:** Mixing reliability fixes (e.g., Connection Pooling/DbHelper) with performance fixes (e.g., `GetOrdinal`) risks rejection due to scope creep and complexity, especially when the reliability fix requires dependencies not easily verified in the diff.
**Action:** Keep performance PRs strictly focused on the loop/algorithm change. Separate architectural cleanup into its own task.
