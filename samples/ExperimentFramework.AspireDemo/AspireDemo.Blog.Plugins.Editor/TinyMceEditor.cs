using System.Text.RegularExpressions;
using AspireDemo.Blog.Contracts;

namespace AspireDemo.Blog.Plugins.Editor;

/// <summary>
/// Full-featured WYSIWYG editor powered by TinyMCE.
/// Rich formatting and media support.
/// </summary>
public sealed partial class TinyMceEditor : IBlogEditorProvider
{
    public string EditorName => "TinyMCE";
    public string EditorDescription => "Full-featured WYSIWYG editor with rich formatting, tables, and media embedding.";
    public EditorType Type => EditorType.WYSIWYG;

    public Task<string> ToHtmlAsync(string content, CancellationToken ct = default)
    {
        // TinyMCE content is already HTML
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
        var sanitized = ScriptTagRegex().Replace(html, "");
        sanitized = OnEventRegex().Replace(sanitized, "");
        sanitized = IframeRegex().Replace(sanitized, match =>
        {
            var src = match.Groups[1].Value;
            // Only allow safe iframe sources
            if (src.Contains("youtube.com") || src.Contains("vimeo.com") || src.Contains("codepen.io"))
                return match.Value;
            return "";
        });
        return Task.FromResult(sanitized);
    }

    public EditorConfig GetConfiguration() => new()
    {
        EditorScript = "https://cdn.tiny.cloud/1/no-api-key/tinymce/6/tinymce.min.js",
        EditorStyles = null,
        InitScript = """
            tinymce.init({
                selector: '#editor',
                plugins: 'anchor autolink charmap codesample emoticons image link lists media searchreplace table visualblocks wordcount',
                toolbar: 'undo redo | blocks fontfamily fontsize | bold italic underline strikethrough | link image media table | align lineheight | numlist bullist indent outdent | emoticons charmap | removeformat',
                height: 500
            });
            """,
        ToolbarFeatures = [
            "undo", "redo", "bold", "italic", "underline", "strikethrough",
            "link", "image", "media", "table", "align", "lists", "code",
            "emoticons", "charmap", "blocks", "fontsize"
        ],
        Options = new Dictionary<string, object>
        {
            ["height"] = 500,
            ["menubar"] = true,
            ["branding"] = false,
            ["promotion"] = false,
            ["content_css"] = "default"
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
        // Add lazy loading and responsive classes to images
        var processed = ImgTagRegex().Replace(content, match =>
        {
            var img = match.Value;
            if (!img.Contains("loading="))
                img = img.Replace("<img ", "<img loading=\"lazy\" ");
            if (!img.Contains("class="))
                img = img.Replace("<img ", "<img class=\"img-fluid\" ");
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

    [GeneratedRegex(@"<iframe[^>]*src=[""']([^""']+)[""'][^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex IframeRegex();

    [GeneratedRegex(@"<img\s+[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ImgTagRegex();
}
