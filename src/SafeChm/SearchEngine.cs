using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace SafeChm;

public sealed record SearchResult(string Title, string RelativePath, string Snippet);

public static class SearchEngine
{
    // Case-insensitive full-text search over extracted .htm/.html files.
    public static List<SearchResult> Search(string dir, string query)
    {
        var results = new List<SearchResult>();
        if (string.IsNullOrWhiteSpace(query)) return results;
        foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext != ".htm" && ext != ".html") continue;
            string html;
            try { html = EncodingDetector.Detect(File.ReadAllBytes(file)).GetString(File.ReadAllBytes(file)); }
            catch { continue; }
            var text = StripTags(html);
            int idx = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;
            var title = ExtractTitle(html);
            if (string.IsNullOrWhiteSpace(title)) title = Path.GetFileName(file);
            results.Add(new SearchResult(title, Path.GetRelativePath(dir, file), MakeSnippet(text, idx, query.Length)));
        }
        return results.OrderBy(r => r.Title, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static string StripTags(string html)
    {
        var text = Regex.Replace(html, @"(?is)<(script|style)\b.*?</\1\s*>", " ");
        text = Regex.Replace(text, @"(?s)<[^>]+>", " ");
        text = WebUtility.HtmlDecode(text);
        text = text.Replace('\u00A0', ' ').Replace("&nbsp;", " ");
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    private static string ExtractTitle(string html)
    {
        var m = Regex.Match(html, @"(?is)<title[^>]*>(.*?)</title>");
        return m.Success ? WebUtility.HtmlDecode(m.Groups[1].Value).Trim() : "";
    }

    private static string MakeSnippet(string text, int idx, int len)
    {
        int start = Math.Max(0, idx - 40);
        int end = Math.Min(text.Length, idx + len + 60);
        return (start > 0 ? "…" : "") + text.Substring(start, end - start) + (end < text.Length ? "…" : "");
    }
}
