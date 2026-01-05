## 2026-01-05 - Global Search Caching
**Learning:** In vanilla JS apps without complex state management, global search features that fetch entire datasets on every keystroke are massive bottlenecks. Simple time-based caching (TTL 60s) effectively eliminates this overhead without requiring complex backend changes.
**Action:** Always check high-frequency event handlers (like search inputs) for network calls and implement debouncing + caching strategies immediately.
