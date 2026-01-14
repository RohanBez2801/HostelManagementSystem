## 2026-01-14 - Debounce Global Search
**Learning:** The global search feature in `dashboard.js` was firing an API call on every keystroke, causing unnecessary load. Standard event handlers in vanilla JS don't debounce by default.
**Action:** Implemented a manual debounce pattern using `setTimeout` and `clearTimeout`. Also added a client-side cache (valid for 60s) for the learner list to further reduce repeated calls. This pattern should be applied to any other live-search inputs in the application (e.g., in other modules).

## 2026-01-14 - Playwright Sync API Blocking
**Learning:** When using `sync_playwright`, `time.sleep()` blocks the entire Python process, including the event loop responsible for handling network interceptions (`page.route`). This caused API calls to hang or not trigger the handler during the sleep period.
**Action:** Always use `page.wait_for_timeout(ms)` in Playwright scripts instead of `time.sleep()`. This pauses the script execution but keeps the browser event loop running, allowing network mocks and other async browser events to process correctly.
