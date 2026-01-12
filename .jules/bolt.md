## 2024-05-22 - Playwright Sync Mode & Time Sleep
**Learning:** In `sync_playwright`, using `time.sleep()` blocks the Python main thread, preventing `page.route` callbacks from executing. The callbacks only run when the Playwright driver process can communicate with the Python process, which requires the Python thread to be free or yielding to Playwright's event loop (via `page.wait_for_timeout` or similar).
**Action:** Always use `page.wait_for_timeout(ms)` instead of `time.sleep(sec)` when waiting for async events like network requests in Playwright scripts.
