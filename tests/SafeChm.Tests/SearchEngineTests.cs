using Xunit;

namespace SafeChm.Tests;

public class SearchEngineTests
{
    [Fact]
    public void StripTags_RemovesTagsAndDecodesEntities()
    {
        var html = "<html><head><title>T</title></head><body><p>Hello &amp; <b>world</b></p>" +
                   "<script>var x=1; alert('hi');</script></body></html>";
        var text = SearchEngine.StripTags(html);
        Assert.Contains("Hello & world", text);
        Assert.DoesNotContain("<", text);
        Assert.DoesNotContain("var x=1", text);
    }

    [Fact]
    public void Search_FindsFile_CaseInsensitive_WithTitleAndSnippet()
    {
        var dir = Path.Combine(Path.GetTempPath(), "safechm-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "sub"));
        File.WriteAllText(Path.Combine(dir, "a.html"),
            "<html><head><title>First Page</title></head><body>This file mentions Archive utilities.</body></html>");
        File.WriteAllText(Path.Combine(dir, "sub", "b.htm"),
            "<html><body>no match here</body></html>");
        try
        {
            var results = SearchEngine.Search(dir, "archive");
            var r = Assert.Single(results);
            Assert.Equal("First Page", r.Title);
            Assert.Contains("Archive", r.Snippet);
            Assert.EndsWith("a.html", r.RelativePath);
        }
        finally { Directory.Delete(dir, true); }
    }
}
