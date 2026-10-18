using Xunit;

namespace SafeChm.Tests;

public class ChmCacheTests
{
    private const string Root = @"C:\cache\abc\";

    [Theory]
    [InlineData(@"..\evil.exe")]
    [InlineData(@"sub\..\..\evil.exe")]
    [InlineData(@"C:\Windows\evil.dll")]
    [InlineData(@"\evil.dll")]
    [InlineData("")]
    public void SafeTarget_RejectsEscapes(string name) => Assert.Null(ChmCache.SafeTarget(Root, name));

    [Theory]
    [InlineData("index.htm", @"C:\cache\abc\index.htm")]
    [InlineData(@"html\a.htm", @"C:\cache\abc\html\a.htm")]
    [InlineData(@"html\..\b.htm", @"C:\cache\abc\b.htm")]
    public void SafeTarget_AllowsInside(string name, string expected) => Assert.Equal(expected, ChmCache.SafeTarget(Root, name));
}
