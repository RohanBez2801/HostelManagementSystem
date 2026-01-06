## 2024-05-22 - [Frontend Search Optimization]
**Learning:** The `globalSearch` function was fetching the entire learner dataset (which can be large) on every keystroke > 1 char. This is a classic N+1-like issue but on the frontend/API boundary.
**Action:** Implemented a 300ms debounce and a 60-second client-side cache. This reduced fetches from N (number of keystrokes) to 1 per session/minute. Future optimizations should consider server-side filtering if the dataset grows too large for client-side filtering.
