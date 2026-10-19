# SafeChm

A safe, script-free viewer for Microsoft HTML Help (.chm) files — a replacement for `hh.exe`, which security guidance (e.g. CISA) recommends blocking because CHM files can run script.

## Features

- Open a `.chm` via the 開啟 button, drag-and-drop, or command-line argument (usable as the `.chm` file handler).
- Extracts the CHM into `%LOCALAPPDATA%\SafeChm\cache\<sha256-of-file>\` (reused across runs); cache folders older than 7 days are deleted on start.
- Table of contents parsed from the `.hhc` sitemap into a tree view (nested `<UL>` supported; encoding auto-detected: `<meta charset>`, then UTF-8, then Big5, then GBK).
- Pages render in WebView2 with **script disabled**, default dialogs, DevTools, web messaging and context menus off, downloads cancelled. The cache folder is served from the virtual host `https://chm.local/` (DenyCors); every navigation and resource request to a non-`chm.local` host is cancelled. External http(s) links ask "在瀏覽器開啟外部連結？" before opening in the default browser. `ms-its:` / `mk:@MSITStore:` links into the same CHM are rewritten to the cache path, everything else is blocked.
- 搜尋: case-insensitive full-text search over extracted `.htm`/`.html` files (tags stripped, entities decoded), with a title + snippet result list; clicking opens the page.
- 匯出 HTML: copies the extracted folder to a folder you choose.
- Status bar permanently shows 腳本已停用.

## Install

Unzip `SafeChm-v1.0.0-win-x64.zip` and run `SafeChm.exe`. Requires the .NET 10 Desktop Runtime and the WebView2 Runtime (both standard on Windows 11).

## Build

```
./build.ps1
```

## Privacy

Reads only the CHM file you open and the extracted files in its own cache folder. It makes no network requests and sends nothing anywhere.

## Licence

MIT. See LICENSE. Third-party components are listed in THIRD-PARTY-NOTICES.md.

---

# SafeChm（繁體中文）

一個安全、停用腳本的 Microsoft HTML Help（.chm）檔案檢視器，用以取代 `hh.exe`。安全指引（例如 CISA）建議封鎖 `hh.exe`，因為 CHM 檔案可以執行腳本。

## 功能

- 透過「開啟」按鈕、拖放或命令列參數開啟 `.chm`（可設為 `.chm` 檔案的預設處理程式）。
- 將 CHM 解壓至 `%LOCALAPPDATA%\SafeChm\cache\<檔案 sha256>\`（重複使用）；啟動時刪除超過 7 天的快取資料夾。
- 從 `.hhc` 站點地圖解析目錄為樹狀結構（支援巢狀 `<UL>`；自動偵測編碼：`<meta charset>`，其次 UTF-8、Big5、GBK）。
- 頁面在 WebView2 中顯示，並**停用腳本**、預設對話框、DevTools、網頁訊息與右鍵選單，取消下載。快取資料夾經由虛擬主機 `https://chm.local/`（DenyCors）提供；所有前往非 `chm.local` 主機的導覽與資源請求一律取消。外部 http(s) 連結會先詢問「在瀏覽器開啟外部連結？」才以預設瀏覽器開啟。指向同一 CHM 的 `ms-its:` / `mk:@MSITStore:` 連結會改寫為快取路徑，其餘一律封鎖。
- 搜尋：對已解壓的 `.htm`/`.html` 檔案進行不分大小寫的全文搜尋（去除標籤、解碼實體），以「標題 + 摘要」清單顯示結果；點擊開啟頁面。
- 匯出 HTML：將已解壓資料夾複製到你所選取的資料夾。
- 狀態列永久顯示「腳本已停用」。

## 安裝

解壓 `SafeChm-v1.0.0-win-x64.zip` 後執行 `SafeChm.exe`。需要 .NET 10 Desktop Runtime 與 WebView2 Runtime（Windows 11 已內建兩者）。

## 建置

```
./build.ps1
```

## 隱私

只讀取你所開啟的 CHM 檔案及其快取資料夾內已解壓的檔案。不會發出任何網路請求，也不傳送任何資料。

## 授權

MIT。詳見 LICENSE。第三方元件列於 THIRD-PARTY-NOTICES.md。

## Sponsor / 贊助

If this tool helps you, you can buy me a coffee. 如果這個工具對你有幫助，歡迎請我喝杯咖啡。

- Buy Me a Coffee: https://buymeacoffee.com/kinfxhk
