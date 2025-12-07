using System;
using System.IO;
using System.Reflection;
using Markdig;
using NoteForge.Interfaces;

namespace NoteForge.Services;

public class MarkdownPreviewService : IMarkdownPreviewService
{
    private readonly MarkdownPipeline _pipeline;
    private readonly string _cssContent;
    private readonly string _htmlTemplate;

    public MarkdownPreviewService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        _cssContent = LoadEmbeddedResource("NoteForge.Templates.MarkdownPreview.css");
        _htmlTemplate = LoadEmbeddedResource("NoteForge.Templates.MarkdownPreview.html");
    }

    public string ConvertToHtml(string markdown)
    {
        return Markdown.ToHtml(markdown ?? string.Empty, _pipeline);
    }

    public string WrapInHtmlDocument(string htmlContent)
    {
        return _htmlTemplate
            .Replace("{{CSS}}", _cssContent)
            .Replace("{{CONTENT}}", htmlContent);
    }

    private static string LoadEmbeddedResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}