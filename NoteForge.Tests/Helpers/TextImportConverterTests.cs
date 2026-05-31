using System.Linq;
using System.Text;
using NoteForge.Helpers;

namespace NoteForge.Tests.Helpers;

public class TextImportConverterTests
{
    [Fact]
    public void TryConvert_utf8_without_bom_decodes()
    {
        var bytes = Encoding.UTF8.GetBytes("héllo");
        Assert.True(TextImportConverter.TryConvert(bytes, out var text));
        Assert.Equal("héllo", text);
    }

    [Fact]
    public void TryConvert_utf8_with_bom_strips_bom()
    {
        var bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes("hi")).ToArray();
        Assert.True(TextImportConverter.TryConvert(bytes, out var text));
        Assert.Equal("hi", text);
    }

    [Fact]
    public void TryConvert_utf16_le_decodes()
    {
        var bytes = new byte[] { 0xFF, 0xFE }.Concat(Encoding.Unicode.GetBytes("hi")).ToArray();
        Assert.True(TextImportConverter.TryConvert(bytes, out var text));
        Assert.Equal("hi", text);
    }

    [Fact]
    public void TryConvert_utf16_be_decodes()
    {
        var bytes = new byte[] { 0xFE, 0xFF }.Concat(Encoding.BigEndianUnicode.GetBytes("hi")).ToArray();
        Assert.True(TextImportConverter.TryConvert(bytes, out var text));
        Assert.Equal("hi", text);
    }

    [Fact]
    public void TryConvert_invalid_utf8_falls_back_to_windows1252()
    {
        var bytes = new byte[] { 0x63, 0x61, 0x66, 0xE9 };
        Assert.True(TextImportConverter.TryConvert(bytes, out var text));
        Assert.Equal("café", text);
    }

    [Fact]
    public void TryConvert_binary_content_is_rejected()
    {
        var bytes = new byte[] { 0x48, 0x00, 0x49 };
        Assert.False(TextImportConverter.TryConvert(bytes, out _));
    }

    [Fact]
    public void TryConvert_normalizes_crlf_and_cr_to_lf()
    {
        var bytes = Encoding.UTF8.GetBytes("a\r\nb\rc\nd");
        Assert.True(TextImportConverter.TryConvert(bytes, out var text));
        Assert.Equal("a\nb\nc\nd", text);
    }

    [Fact]
    public void TryConvert_empty_input_succeeds_with_empty_text()
    {
        Assert.True(TextImportConverter.TryConvert([], out var text));
        Assert.Equal("", text);
    }

    [Theory]
    [InlineData("note.txt", true)]
    [InlineData("note.md", true)]
    [InlineData("a.markdown", true)]
    [InlineData("a.LOG", true)]
    [InlineData("a.pdf", false)]
    [InlineData("a.docx", false)]
    [InlineData("noext", false)]
    public void IsSupported_matches_whitelist(string path, bool expected)
    {
        Assert.Equal(expected, TextImportConverter.IsSupported(path));
    }
}
