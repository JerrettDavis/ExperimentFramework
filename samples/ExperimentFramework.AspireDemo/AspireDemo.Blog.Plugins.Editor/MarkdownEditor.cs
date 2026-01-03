using System.Text.RegularExpressions;
using AspireDemo.Blog.Contracts;
using Markdig;

namespace AspireDemo.Blog.Plugins.Editor;

/// <summary>
/// Developer-friendly markdown editor with live preview.
/// Uses Markdig for rendering.
/// </summary>
public sealed partial class MarkdownEditor : IBlogEditorProvider
{
    public string EditorName => "Markdown";
    public string EditorDescription => "Clean, developer-friendly markdown with live preview. Perfect for technical writing.";
    public EditorType Type => EditorType.Markdown;

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseAutoLinks()
        .UseTaskLists()
        .UseEmojiAndSmiley()
        .Build();

    public Task<string> ToHtmlAsync(string content, CancellationToken ct = default)
    {
        var html = Markdown.ToHtml(content, Pipeline);
        return Task.FromResult(html);
    }

    public Task<string> ToPlainTextAsync(string content, CancellationToken ct = default)
    {
        var plain = Markdown.ToPlainText(content, Pipeline);
        return Task.FromResult(plain);
    }

    public Task<string> SanitizeAsync(string html, CancellationToken ct = default)
    {
        // Basic sanitization - in production use a proper sanitizer like HtmlSanitizer
        var sanitized = ScriptTagRegex().Replace(html, "");
        sanitized = OnEventRegex().Replace(sanitized, "");
        return Task.FromResult(sanitized);
    }

    public EditorConfig GetConfiguration() => new()
    {
        EditorScript = null, // Use built-in textarea
        EditorStyles = null,
        InitScript = null,
        ToolbarFeatures = ["bold", "italic", "link", "code", "quote", "list", "heading", "image"],
        Options = new Dictionary<string, object>
        {
            ["mode"] = "markdown",
            ["lineNumbers"] = true,
            ["lineWrapping"] = true,
            ["preview"] = true
        }
    };

    public Task<string> GenerateExcerptAsync(string content, int maxLength = 200, CancellationToken ct = default)
    {
        var plain = Markdown.ToPlainText(content, Pipeline);

        if (plain.Length <= maxLength)
            return Task.FromResult(plain.Trim());

        var excerpt = plain[..maxLength];
        var lastSpace = excerpt.LastIndexOf(' ');
        if (lastSpace > 0)
            excerpt = excerpt[..lastSpace];

        return Task.FromResult(excerpt.Trim() + "...");
    }

    public Task<string> ProcessImagesAsync(string content, CancellationToken ct = default)
    {
        // Add lazy loading to images
        var processed = ImageRegex().Replace(content, match =>
        {
            var alt = match.Groups[1].Value;
            var url = match.Groups[2].Value;
            return $"![{alt}]({url}){{loading=lazy}}";
        });
        return Task.FromResult(processed);
    }

    public int CalculateReadTime(string content)
    {
        var plain = Markdown.ToPlainText(content, Pipeline);
        var words = plain.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Ceiling(words / 200.0));
    }

    [GeneratedRegex(@"<script[^>]*>.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptTagRegex();

    [GeneratedRegex(@"\s+on\w+\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex OnEventRegex();

    [GeneratedRegex(@"!\[([^\]]*)\]\(([^\)]+)\)")]
    private static partial Regex ImageRegex();
}
