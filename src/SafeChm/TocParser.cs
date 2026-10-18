using System.Text.RegularExpressions;

namespace SafeChm;

public sealed record TocEntry(string Name, string Local)
{
    public List<TocEntry> Children { get; } = new();
}

public static class TocParser
{
    // Parse an .hhc sitemap (OBJECT/text-sitemap blocks nested in UL/LI) into a tree.
    public static List<TocEntry> Parse(string hhc)
    {
        var items = new List<(int Depth, string Name, string Local)>();
        int depth = 0;
        var token = new Regex(@"(?is)<ul\b|</ul\s*>|<object\b.*?</object\s*>");
        foreach (Match m in token.Matches(hhc))
        {
            var t = m.Value;
            if (t.StartsWith("<ul", StringComparison.OrdinalIgnoreCase)) { depth++; continue; }
            if (t.StartsWith("</ul", StringComparison.OrdinalIgnoreCase)) { if (depth > 0) depth--; continue; }
            items.Add((depth, ParamValue(t, "Name"), ParamValue(t, "Local")));
        }
        return BuildTree(items);
    }

    public static int CountEntries(IEnumerable<TocEntry> entries) =>
        entries.Sum(e => 1 + CountEntries(e.Children));

    private static string ParamValue(string obj, string paramName)
    {
        var tag = Regex.Match(obj,
            $@"<param\s+[^>]*name\s*=\s*[""']?\s*{Regex.Escape(paramName)}\s*[""']?[^>]*>",
            RegexOptions.IgnoreCase);
        if (!tag.Success) return "";
        var v = Regex.Match(tag.Value,
            @"value\s*=\s*(""(?<v>[^""]*)""|'(?<v>[^']*)'|(?<v>[^\s>]+))",
            RegexOptions.IgnoreCase);
        return v.Success ? v.Groups["v"].Value : "";
    }

    private static List<TocEntry> BuildTree(List<(int Depth, string Name, string Local)> items)
    {
        var root = new TocEntry("", "");
        var stack = new Stack<(int Depth, TocEntry Node)>();
        stack.Push((-1, root));
        foreach (var (d, name, local) in items)
        {
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(local)) continue;
            while (stack.Peek().Depth >= d) stack.Pop();
            var child = new TocEntry(name, local);
            stack.Peek().Node.Children.Add(child);
            stack.Push((d, child));
        }
        return root.Children;
    }
}
