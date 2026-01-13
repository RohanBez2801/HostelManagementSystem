## 2024-05-22 - Global Search Bottleneck
**Learning:** The application was performing a full database fetch (`/api/learner/list-all`) on every keystroke in the global search bar. This is a classic N+1-like issue but on the network layer, where N is the number of characters typed.
**Action:** Implemented a debounce (300ms) and client-side caching (60s) pattern. For future reference, always check `onkeyup` handlers for expensive operations and apply debouncing by default.
