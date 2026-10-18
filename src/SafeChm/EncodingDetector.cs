using System.Text;
using System.Text.RegularExpressions;

namespace SafeChm;

public static class EncodingDetector
{
    static EncodingDetector()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    // Detect charset from <meta charset>, else strict UTF-8, then Big5 (950), then GBK (936).
    public static Encoding Detect(byte[] bytes)
    {
        var head = Encoding.Latin1.GetString(bytes, 0, Math.Min(bytes.Length, 4096));
        var m = Regex.Match(head, @"charset\s*=\s*[""']?\s*([A-Za-z0-9_-]+)", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            try { return Encoding.GetEncoding(m.Groups[1].Value); }
            catch (ArgumentException) { }
        }

        try
        {
            new UTF8Encoding(false, true).GetString(bytes);
            return new UTF8Encoding(false);
        }
        catch (DecoderFallbackException) { }

        try { return Encoding.GetEncoding(950); } // Big5
        catch (ArgumentException) { }
        return Encoding.GetEncoding(936); // GBK
    }
}
