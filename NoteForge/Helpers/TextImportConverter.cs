using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NoteForge.Helpers;

public static class TextImportConverter
{
    private static readonly HashSet<string> _supported = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".text", ".log", ".md", ".markdown", ".mdown", ".mkd"
    };

    static TextImportConverter()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static IReadOnlyCollection<string> SupportedExtensions => _supported;

    public static bool IsSupported(string path) => _supported.Contains(Path.GetExtension(path));

    public static bool TryConvert(byte[] bytes, out string text)
    {
        text = string.Empty;
        if (bytes is null)
            return false;

        string decoded;

        if (bytes.Length >= 3 && bytes[0] is 0xEF && bytes[1] is 0xBB && bytes[2] is 0xBF)
        {
            decoded = new UTF8Encoding(false).GetString(bytes, 3, bytes.Length - 3);
        }
        else if (bytes.Length >= 2 && bytes[0] is 0xFF && bytes[1] is 0xFE)
        {
            decoded = Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        }
        else if (bytes.Length >= 2 && bytes[0] is 0xFE && bytes[1] is 0xFF)
        {
            decoded = Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
        }
        else
        {
            if (Array.IndexOf(bytes, (byte)0x00) >= 0)
                return false;

            try
            {
                decoded = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                decoded = Encoding.GetEncoding(1252).GetString(bytes);
            }
        }

        text = decoded.Replace("\r\n", "\n").Replace("\r", "\n");
        return true;
    }
}
