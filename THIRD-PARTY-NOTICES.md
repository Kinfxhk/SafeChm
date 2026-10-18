# Third-party notices

SafeChm uses the following third-party components.

| Component | Licence | Source |
|-----------|---------|--------|
| Microsoft.Web.WebView2 | MIT | https://www.nuget.org/packages/Microsoft.Web.WebView2 |
| SevenZipExtractor | MIT | https://github.com/adoconnection/SevenZipExtractor |
| 7z.dll / 7z64.dll (bundled, used by SevenZipExtractor) | GNU LGPL (with the unRAR restriction) | https://www.7-zip.org/ |
| xUnit.net (test-only) | Apache-2.0 | https://xunit.net/ |
| .NET 10 | MIT | https://dotnet.microsoft.com/ |

The `7z.dll` / `7z64.dll` binaries are produced by the 7-Zip project (Igor Pavlov),
distributed under the GNU LGPL; the unRAR restriction in 7-Zip's license applies to
the RAR code, which SafeChm does not use (it only opens CHM).
