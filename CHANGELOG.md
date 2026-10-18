# Changelog

## 1.0.0 - 2026-10-19

Initial release.

- Open `.chm` via button, drag-and-drop, or command-line argument.
- Extract CHM to `%LOCALAPPDATA%\SafeChm\cache\<sha256>\`; prune cache dirs older than 7 days.
- Table of contents from the `.hhc` sitemap (nested `<UL>`, encoding auto-detection).
- Script-disabled WebView2 rendering served from the `https://chm.local/` virtual host; external links blocked behind a confirmation dialog; `ms-its:`/`mk:@MSITStore:` links rewritten or blocked.
- Full-text search with title + snippet results.
- 匯出 HTML (copy extracted folder).
- Status bar "腳本已停用".
