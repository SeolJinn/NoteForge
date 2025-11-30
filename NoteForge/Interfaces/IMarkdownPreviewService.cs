namespace NoteForge.Interfaces;

public interface IMarkdownPreviewService
{
    string ConvertToHtml(string markdown);
    string WrapInHtmlDocument(string htmlContent);
}