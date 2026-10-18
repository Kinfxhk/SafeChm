using System.Text;
using Xunit;

namespace SafeChm.Tests;

public class TocParserTests
{
    static TocParserTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Fact]
    public void Parses_Nested_Ul_Sitemap()
    {
        const string hhc = """
            <HTML><BODY>
            <OBJECT type="text/sitemap">
                <param name="Name" value="Book">
                <param name="Local" value="">
            </OBJECT>
            <UL>
                <LI><OBJECT type="text/sitemap">
                    <param name="Name" value="Chapter 1">
                    <param name="Local" value="ch1.htm">
                </OBJECT>
                <UL>
                    <LI><OBJECT type="text/sitemap">
                        <param name="Name" value="Section 1.1">
                        <param name="Local" value="s1.htm">
                    </OBJECT></LI>
                </UL>
                </LI>
                <LI><OBJECT type="text/sitemap">
                    <param name="Name" value="Chapter 2">
                    <param name="Local" value="ch2.htm">
                </OBJECT></LI>
            </UL>
            </BODY></HTML>
            """;

        var entries = TocParser.Parse(hhc);

        var book = Assert.Single(entries);
        Assert.Equal("Book", book.Name);
        Assert.Equal(2, book.Children.Count);
        Assert.Equal("Chapter 1", book.Children[0].Name);
        var section = Assert.Single(book.Children[0].Children);
        Assert.Equal("Section 1.1", section.Name);
        Assert.Equal("Chapter 2", book.Children[1].Name);
        Assert.Equal(4, TocParser.CountEntries(entries));
    }

    [Fact]
    public void Parses_Sitemap_With_Leading_Ul_No_Root_Object()
    {
        const string hhc = """
            <HTML><BODY>
            <UL>
                <LI><OBJECT type="text/sitemap">
                    <param name="Name" value="A">
                    <param name="Local" value="a.htm">
                </OBJECT></LI>
                <LI><OBJECT type="text/sitemap">
                    <param name="Name" value="B">
                    <param name="Local" value="b.htm">
                </OBJECT></LI>
            </UL>
            </BODY></HTML>
            """;

        var entries = TocParser.Parse(hhc);
        Assert.Equal(2, entries.Count);
        Assert.Equal("A", entries[0].Name);
        Assert.Equal("B", entries[1].Name);
    }

    [Fact]
    public void Parses_Big5_Sitemap_Bytes()
    {
        // "第一章" encoded in Big5 (950), no <meta charset> present.
        var big5 = Encoding.GetEncoding(950);
        var name = "第一章";
        var hhc = $"<OBJECT type=\"text/sitemap\"><param name=\"Name\" value=\"{name}\">" +
                  "<param name=\"Local\" value=\"ch1.htm\"></OBJECT>";
        var bytes = big5.GetBytes(hhc);

        var text = EncodingDetector.Detect(bytes).GetString(bytes);
        var entries = TocParser.Parse(text);

        var entry = Assert.Single(entries);
        Assert.Equal("第一章", entry.Name);
        Assert.Equal("ch1.htm", entry.Local);
    }
}
