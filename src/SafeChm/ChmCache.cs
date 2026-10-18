using System.IO;
using System.Security.Cryptography;
using SevenZipExtractor;

namespace SafeChm;

public static class ChmCache
{
    public static string Root =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SafeChm");
    public static string CacheRoot => Path.Combine(Root, "cache");
    public static string WebView2Data => Path.Combine(Root, "webview2");

    public static string DirFor(string chmPath)
    {
        var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(chmPath))).ToLowerInvariant();
        return Path.Combine(CacheRoot, hash);
    }

    // Extract the CHM into cache/<sha256>, reusing an existing extraction.
    public static string Extract(string chmPath)
    {
        var dir = DirFor(chmPath);
        if (Directory.Exists(dir)) return dir;
        var tmp = dir + ".tmp";
        if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
        Directory.CreateDirectory(tmp);
        var root = Path.GetFullPath(tmp) + Path.DirectorySeparatorChar;
        using (var archive = new ArchiveFile(chmPath))
            archive.Extract(entry => SafeTarget(root, entry.FileName));
        Directory.Move(tmp, dir);
        return dir;
    }

    // CHM is untrusted input: refuse entries that would land outside the cache dir (zip-slip).
    public static string? SafeTarget(string root, string entryName)
    {
        if (string.IsNullOrEmpty(entryName) || Path.IsPathRooted(entryName)) return null;
        var full = Path.GetFullPath(Path.Combine(root, entryName));
        return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full : null;
    }

    public static void Cleanup(int maxAgeDays = 7)
    {
        try
        {
            if (!Directory.Exists(CacheRoot)) return;
            var cutoff = DateTime.Now.AddDays(-maxAgeDays);
            foreach (var d in Directory.EnumerateDirectories(CacheRoot))
            {
                try { if (Directory.GetLastWriteTime(d) < cutoff) Directory.Delete(d, true); }
                catch { }
            }
        }
        catch { }
    }
}
