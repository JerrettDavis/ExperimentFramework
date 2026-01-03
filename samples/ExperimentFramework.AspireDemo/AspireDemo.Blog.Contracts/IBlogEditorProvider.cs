namespace AspireDemo.Blog.Contracts;

/// <summary>
/// Plugin interface for content editing and rendering.
/// Implementations provide different editing experiences (Markdown, TinyMCE, Quill, etc.)
/// </summary>
public interface IBlogEditorProvider
{
    /// <summary>Display name of the editor.</summary>
    string EditorName { get; }

    /// <summary>Description of the editor's characteristics.</summary>
    string EditorDescription { get; }

    /// <summary>Type of editor (Markdown, RichText, WYSIWYG).</summary>
    EditorType Type { get; }

    /// <summary>Converts editor content to HTML for display.</summary>
    Task<string> ToHtmlAsync(string content, CancellationToken ct = default);

    /// <summary>Converts content to plain text.</summary>
    Task<string> ToPlainTextAsync(string content, CancellationToken ct = default);

    /// <summary>Sanitizes HTML to prevent XSS.</summary>
    Task<string> SanitizeAsync(string html, CancellationToken ct = default);

    /// <summary>Gets the client-side configuration for the editor.</summary>
    EditorConfig GetConfiguration();

    /// <summary>Generates an excerpt from the content.</summary>
    Task<string> GenerateExcerptAsync(string content, int maxLength = 200, CancellationToken ct = default);

    /// <summary>Processes images in content (optimization, CDN URLs, etc.).</summary>
    Task<string> ProcessImagesAsync(string content, CancellationToken ct = default);

    /// <summary>Calculates estimated read time in minutes.</summary>
    int CalculateReadTime(string content);
}

/// <summary>
/// Type of content editor.
/// </summary>
public enum EditorType
{
    /// <summary>Plain markdown with optional preview.</summary>
    Markdown,

    /// <summary>Rich text editor with formatting toolbar.</summary>
    RichText,

    /// <summary>Full WYSIWYG editor.</summary>
    WYSIWYG
}

/// <summary>
/// Client-side editor configuration.
/// </summary>
public record EditorConfig
{
    /// <summary>CDN URL for the editor's JavaScript.</summary>
    public string? EditorScript { get; init; }

    /// <summary>CDN URL for the editor's CSS.</summary>
    public string? EditorStyles { get; init; }

    /// <summary>Initialization script to run after loading.</summary>
    public string? InitScript { get; init; }

    /// <summary>Editor-specific options as JSON.</summary>
    public Dictionary<string, object> Options { get; init; } = [];

    /// <summary>Supported toolbar features.</summary>
    public List<string> ToolbarFeatures { get; init; } = [];
}
