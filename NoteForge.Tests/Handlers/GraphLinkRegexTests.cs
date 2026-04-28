using System.Text.RegularExpressions;

namespace NoteForge.Tests.Handlers;

public class GraphLinkRegexTests
{
    private static readonly Regex MarkdownLink = new(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled);
    private static readonly Regex WikiLink = new(@"\[\[([^\]]+)\]\]", RegexOptions.Compiled);

    [Fact]
    public void MarkdownLink_extracts_text_and_target()
    {
        var match = MarkdownLink.Match("see [my note](other.md) for details");
        Assert.True(match.Success);
        Assert.Equal("my note", match.Groups[1].Value);
        Assert.Equal("other.md", match.Groups[2].Value);
    }

    [Fact]
    public void MarkdownLink_extracts_multiple_links()
    {
        var matches = MarkdownLink.Matches("[a](one.md) and [b](two.md) plus [c](three.md)");
        Assert.Equal(3, matches.Count);
        Assert.Equal("one.md", matches[0].Groups[2].Value);
        Assert.Equal("two.md", matches[1].Groups[2].Value);
        Assert.Equal("three.md", matches[2].Groups[2].Value);
    }

    [Fact]
    public void MarkdownLink_extracts_relative_paths()
    {
        var match = MarkdownLink.Match("[note](../sibling/note.md)");
        Assert.True(match.Success);
        Assert.Equal("../sibling/note.md", match.Groups[2].Value);
    }

    [Fact]
    public void MarkdownLink_does_not_match_image_syntax_alone()
    {
        var match = MarkdownLink.Match("![alt](img.png)");
        Assert.True(match.Success);
        Assert.Equal("alt", match.Groups[1].Value);
        Assert.Equal("img.png", match.Groups[2].Value);
    }

    [Fact]
    public void MarkdownLink_does_not_match_unclosed_brackets()
    {
        Assert.False(MarkdownLink.Match("[unclosed text").Success);
        Assert.False(MarkdownLink.Match("(no-brackets.md)").Success);
        Assert.False(MarkdownLink.Match("[text](no-paren-close").Success);
    }

    [Fact]
    public void WikiLink_extracts_link_name()
    {
        var match = WikiLink.Match("see [[Other Note]] for context");
        Assert.True(match.Success);
        Assert.Equal("Other Note", match.Groups[1].Value);
    }

    [Fact]
    public void WikiLink_extracts_multiple_links()
    {
        var matches = WikiLink.Matches("link to [[First]] and [[Second]] and [[Third]]");
        Assert.Equal(3, matches.Count);
        Assert.Equal("First", matches[0].Groups[1].Value);
        Assert.Equal("Second", matches[1].Groups[1].Value);
        Assert.Equal("Third", matches[2].Groups[1].Value);
    }

    [Fact]
    public void WikiLink_handles_unicode_in_name()
    {
        var match = WikiLink.Match("[[Rețetă Sarmale]]");
        Assert.True(match.Success);
        Assert.Equal("Rețetă Sarmale", match.Groups[1].Value);
    }

    [Fact]
    public void WikiLink_does_not_match_single_brackets()
    {
        Assert.False(WikiLink.Match("[Just one bracket]").Success);
    }

    [Fact]
    public void WikiLink_does_not_match_unclosed()
    {
        Assert.False(WikiLink.Match("[[Unclosed").Success);
    }

    [Fact]
    public void Regexes_do_not_overlap_on_wiki_link()
    {
        var text = "[[Wiki Link]]";
        Assert.False(MarkdownLink.Match(text).Success);
        Assert.True(WikiLink.Match(text).Success);
    }
}
