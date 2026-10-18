# HANDOFF

## What was built

SafeChm v1.0.0 — a .NET 10 WPF viewer for `.chm` (Microsoft HTML Help) files, replacing `hh.exe`.
Single project `src/SafeChm`, tests `tests/SafeChm.Tests` (xUnit). NuGet: `Microsoft.Web.WebView2` (1.0.4258.31)
and `SevenZipExtractor` (1.0.19, bundles 7z.dll) only, plus xUnit test packages.

Features shipped (exactly the v1.0.0 list):
1. Open via 開啟 button, drag-drop, or command-line arg (usable as the `.chm` file handler).
2. Extract to `%LOCALAPPDATA%\SafeChm\cache\<sha256>\` (reused); prune cache dirs older than 7 days on start.
3. TOC parsed from `.hhc` sitemap into a TreeView (nested `<UL>`); encoding: `<meta charset>` → UTF-8 → Big5(950) → GBK(936), `CodePagesEncodingProvider` registered.
4. WebView2 with script disabled, default dialogs/DevTools/web-message/context-menu off, downloads cancelled. Cache served as virtual host `https://chm.local/` (DenyCors). Non-`chm.local` navigation and resource requests cancelled; external http(s) asks "在瀏覽器開啟外部連結？"; `ms-its:`/`mk:@MSITStore:` links into the same CHM rewritten to cache path, else blocked.
5. 搜尋: case-insensitive full-text search over `.htm`/`.html` (tags stripped, entities decoded), title+snippet results; click opens page.
6. 匯出 HTML: copies extracted folder to a user-chosen folder.
7. Status bar permanently "腳本已停用".

## Commands run (real output)

- `dotnet build src/SafeChm/SafeChm.csproj -c Release` → 建置成功, 0 warnings, 0 errors.
- `dotnet test tests/SafeChm.Tests/SafeChm.Tests.csproj -c Release` →
  `已通過! - 失敗: 0, 通過: 5, 略過: 0, 總計: 5` (TocParserTests x3, SearchEngineTests x2).
- `powershell -File build.ps1` → exit 0, tests pass, zip at
  `dist/SafeChm-v1.0.0-win-x64.zip` (2,168,602 bytes).
- Selftest (`dist/publish/SafeChm.exe --selftest "C:\Program Files\7-Zip\7-zip.chm" --out evidence/selftest.txt`):
  extracted 94 files, 70 TOC entries, 59 results for "archive", exit 0. Output saved to `evidence/selftest.txt`.
- GUI launch with `7-zip.chm` for ~7s, screenshot captured via PowerShell `System.Drawing CopyFromScreen`
  of the window bounds (1800x1200 physical px, DPI 150%) to `evidence/gui.png` (147,603 bytes), then closed.
  Screenshot verified: title "SafeChm - 7-zip.chm", TOC tree populated, 7-Zip start page rendered,
  status bar shows 腳本已停用.

## What I could NOT verify

- External-link confirmation dialog and `ms-its:` rewriting were implemented but not manually exercised
  (the test CHM has no external http links; behaviour verified by code review only).
- Script-disabled rendering was confirmed by configuration, not by observing a scripted CHM actually fail to run.
- 匯出 HTML folder picker and drag-drop were not clicked in the GUI session.

## Known limits / notes

- No index (.hhk) tab, bookmarks, printing, settings, themes, multi-tab, auto-update or installer (per "Not in v1").
- Encoding fallback is literal per spec: meta → UTF-8 → Big5 → GBK. A GBK-encoded CHM without meta would be
  decoded as Big5; Big5 is tried first per spec.
- Search reads each `.htm`/`.html` fully into memory; fine for CHM-sized docs, not for huge file sets.
- Selftest writes its report to the `--out` file (and stdout); when `--out` is omitted it writes `selftest.txt`
  in the working directory.

## Deletions / choices (reversible)

- Used the SevenZipExtractor NuGet package's own `.targets`, which copies `7z.dll` to `x64/` and `x86/`
  under the output; the library locates `x64/7z.dll` at runtime. No binaries committed to the repo.
- `--selftest` is a hidden CLI mode handled in `App.OnStartup` (exits before any window is shown).


## Reviewer verification (it-dog)

- KinTest VM (clean Windows 11, .NET 10 Desktop Runtime, release zip only): `--selftest 7-zip.chm` exit 0 (94 files, 70 TOC entries, 59 hits); GUI opened the CHM, TOC + page rendered, status bar 腳本已停用. Evidence: `evidence/vm/`.
