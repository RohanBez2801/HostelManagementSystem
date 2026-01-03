## 2026-01-03 - [Missing Frontend Optimization Patterns]
**Learning:** The existing codebase lacked standard frontend optimizations like debouncing and caching for the global search, which caused a full database fetch on every keystroke. This highlights a pattern of potential "naive" implementation in the frontend that needs checking.
**Action:** When working on frontend features in this repo, always check for missing basic optimizations (debounce, cache, memoization) in event handlers.
