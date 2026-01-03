using System.Text.RegularExpressions;
using AspireDemo.Blog.Contracts;

namespace AspireDemo.Blog.Plugins.Editor;

/// <summary>
/// Modern, lightweight rich text editor powered by Quill.
/// Clean output with customizable formats.
/// </summary>
public sealed partial class QuillEditor : IBlogEditorProvider
{
    public string EditorName => "Quill";
    public string EditorDescription => "Modern, lightweight rich text editor with clean output and smooth editing experience.";
    public EditorType Type => EditorType.RichText;

    public Task<string> ToHtmlAsync(string content, CancellationToken ct = default)
    {
        // Quill stores content as HTML (or Delta format which we'd convert)
        return Task.FromResult(content);
    }

    public Task<string> ToPlainTextAsync(string content, CancellationToken ct = default)
    {
        var plain = HtmlTagRegex().Replace(content, " ");
        plain = WhitespaceRegex().Replace(plain, " ");
        return Task.FromResult(plain.Trim());
    }

    public Task<string> SanitizeAsync(string html, CancellationToken ct = default)
    {
        // Quill produces relatively clean output, but still sanitize
        var sanitized = ScriptTagRegex().Replace(html, "");
        sanitized = OnEventRegex().Replace(sanitized, "");
        return Task.FromResult(sanitized);
    }

    public EditorConfig GetConfiguration() => new()
    {
        EditorScript = "https://cdn.quilljs.com/1.3.7/quill.min.js",
        EditorStyles = "https://cdn.quilljs.com/1.3.7/quill.snow.css",
        InitScript = """
            var quill = new Quill('#editor', {
                theme: 'snow',
                modules: {
                    toolbar: [
                        [{ 'header': [1, 2, 3, false] }],
                        ['bold', 'italic', 'underline', 'strike'],
                        ['blockquote', 'code-block'],
                        [{ 'list': 'ordered'}, { 'list': 'bullet' }],
                        ['link', 'image'],
                        ['clean']
                    ]
                }
            });
            """,
        ToolbarFeatures = [
            "header", "bold", "italic", "underline", "strike",
            "blockquote", "code-block", "list", "link", "image", "clean"
        ],
        Options = new Dictionary<string, object>
        {
            ["theme"] = "snow",
            ["placeholder"] = "Start writing your post...",
            ["formats"] = new[]
            {
                "header", "bold", "italic", "underline", "strike",
                "blockquote", "code-block", "list", "bullet",
                "link", "image"
            }
        }
    };

    public Task<string> GenerateExcerptAsync(string content, int maxLength = 200, CancellationToken ct = default)
    {
        var plain = HtmlTagRegex().Replace(content, " ");
        plain = WhitespaceRegex().Replace(plain, " ").Trim();

        if (plain.Length <= maxLength)
            return Task.FromResult(plain);

        var excerpt = plain[..maxLength];
        var lastSpace = excerpt.LastIndexOf(' ');
        if (lastSpace > 0)
            excerpt = excerpt[..lastSpace];

        return Task.FromResult(excerpt.Trim() + "...");
    }

    public Task<string> ProcessImagesAsync(string content, CancellationToken ct = default)
    {
        // Add lazy loading to images
        var processed = ImgTagRegex().Replace(content, match =>
        {
            var img = match.Value;
            if (!img.Contains("loading="))
                img = img.Replace("<img ", "<img loading=\"lazy\" ");
            return img;
        });
        return Task.FromResult(processed);
    }

    public int CalculateReadTime(string content)
    {
        var plain = HtmlTagRegex().Replace(content, " ");
        var words = plain.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Ceiling(words / 200.0));
    }

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"<script[^>]*>.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptTagRegex();

    [GeneratedRegex(@"\s+on\w+\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex OnEventRegex();

    [GeneratedRegex(@"<img\s+[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ImgTagRegex();
}
