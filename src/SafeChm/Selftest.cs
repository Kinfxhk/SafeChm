using System.IO;
using System.Text;

namespace SafeChm;

public static class Selftest
{
    public static int Run(string chmPath, string? outFile)
    {
        var sb = new StringBuilder();
        int exit = 0;
        try
        {
            ChmCache.Cleanup();
            sb.AppendLine($"CHM: {chmPath}");
            var dir = ChmCache.Extract(chmPath);
            int files = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Count();
            sb.AppendLine($"Extracted files: {files}");
            sb.AppendLine($"Cache dir: {dir}");

            var hhc = Directory.EnumerateFiles(dir, "*.hhc", SearchOption.AllDirectories).FirstOrDefault();
            if (hhc != null)
            {
                var bytes = File.ReadAllBytes(hhc);
                var text = EncodingDetector.Detect(bytes).GetString(bytes);
                var toc = TocParser.Parse(text);
                sb.AppendLine($"TOC entries: {TocParser.CountEntries(toc)}");
                sb.AppendLine($"TOC file: {Path.GetFileName(hhc)}");
            }
            else
            {
                sb.AppendLine("TOC entries: 0 (no .hhc found)");
            }

            var results = SearchEngine.Search(dir, "archive");
            sb.AppendLine($"Search 'archive' results: {results.Count}");
            foreach (var r in results.Take(10))
                sb.AppendLine($"  - {r.Title}  [{r.RelativePath}]");
            if (results.Count > 10) sb.AppendLine($"  ... and {results.Count - 10} more");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"SELFTEST FAILED: {ex}");
            exit = 1;
        }

        var report = sb.ToString();
        Console.WriteLine(report);
        try { File.WriteAllText(outFile ?? "selftest.txt", report, new UTF8Encoding(false)); }
        catch { }
        return exit;
    }
}
