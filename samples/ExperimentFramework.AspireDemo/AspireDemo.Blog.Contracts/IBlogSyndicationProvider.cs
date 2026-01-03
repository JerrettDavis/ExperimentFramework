namespace AspireDemo.Blog.Contracts;

/// <summary>
/// Plugin interface for cross-posting to external platforms.
/// Implementations handle publishing to dev.to, Hashnode, Medium, etc.
/// </summary>
public interface IBlogSyndicationProvider
{
    /// <summary>Display name of the platform.</summary>
    string PlatformName { get; }

    /// <summary>Base URL of the platform.</summary>
    string PlatformUrl { get; }

    /// <summary>Icon identifier for the platform (for UI display).</summary>
    string PlatformIcon { get; }

    /// <summary>Platform brand color (hex).</summary>
    string PlatformColor { get; }

    /// <summary>Publishes a post to the platform.</summary>
    Task<SyndicationResult> PublishAsync(BlogPost post, SyndicationOptions options, CancellationToken ct = default);

    /// <summary>Updates an already-published post.</summary>
    Task<SyndicationResult> UpdateAsync(string externalId, BlogPost post, CancellationToken ct = default);

    /// <summary>Deletes/unpublishes a post from the platform.</summary>
    Task<bool> DeleteAsync(string externalId, CancellationToken ct = default);

    /// <summary>Gets the current status of a published post.</summary>
    Task<SyndicationStatus> GetStatusAsync(string externalId, CancellationToken ct = default);

    /// <summary>Validates that credentials are configured correctly.</summary>
    Task<ValidationResult> ValidateCredentialsAsync(CancellationToken ct = default);

    /// <summary>Gets the capabilities supported by this platform.</summary>
    SyndicationCapabilities GetCapabilities();
}

/// <summary>
/// Options for publishing to an external platform.
/// </summary>
public record SyndicationOptions
{
    /// <summary>Publish as draft (if supported).</summary>
    public bool PublishAsDraft { get; init; }

    /// <summary>Include canonical URL pointing back to original.</summary>
    public bool IncludeCanonicalUrl { get; init; } = true;

    /// <summary>The canonical URL of the original post.</summary>
    public string? CanonicalUrl { get; init; }

    /// <summary>Override tags for this platform.</summary>
    public List<string>? Tags { get; init; }

    /// <summary>Schedule for future publication (if supported).</summary>
    public DateTime? ScheduledFor { get; init; }
}

/// <summary>
/// Result of a syndication operation.
/// </summary>
public record SyndicationResult(
    bool Success,
    string? ExternalId,
    string? ExternalUrl,
    string? Error);

/// <summary>
/// Current status of a syndicated post.
/// </summary>
public record SyndicationStatus
{
    public bool IsLive { get; init; }
    public int Views { get; init; }
    public int Reactions { get; init; }
    public int Comments { get; init; }
    public DateTime? PublishedAt { get; init; }
    public string? ExternalUrl { get; init; }
}

/// <summary>
/// Validation result for credentials/configuration.
/// </summary>
public record ValidationResult(bool IsValid, string? Error);

/// <summary>
/// Capabilities supported by a syndication platform.
/// </summary>
public record SyndicationCapabilities
{
    /// <summary>Supports publishing as draft first.</summary>
    public bool SupportsDraft { get; init; }

    /// <summary>Supports scheduling future publication.</summary>
    public bool SupportsScheduling { get; init; }

    /// <summary>Supports canonical URL for SEO.</summary>
    public bool SupportsCanonicalUrl { get; init; }

    /// <summary>Supports updating after publication.</summary>
    public bool SupportsUpdate { get; init; }

    /// <summary>Supports deleting/unpublishing.</summary>
    public bool SupportsDelete { get; init; }

    /// <summary>Maximum title length allowed.</summary>
    public int MaxTitleLength { get; init; }

    /// <summary>Maximum number of tags allowed.</summary>
    public int MaxTags { get; init; }

    /// <summary>Whether images need special handling.</summary>
    public bool RequiresImageUpload { get; init; }
}
