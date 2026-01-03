using AspireDemo.Blog.Contracts;

namespace AspireDemo.Blog.Plugins.Syndication;

/// <summary>
/// Syndicates content to DEV.to (dev.to).
/// Simulated for demo purposes.
/// </summary>
public sealed class DevToSyndicator : IBlogSyndicationProvider
{
    public string PlatformName => "DEV Community";
    public string PlatformUrl => "https://dev.to";
    public string PlatformIcon => "devto";
    public string PlatformColor => "#0a0a0a";

    private readonly Dictionary<string, (string url, DateTime publishedAt, int views, int reactions)> _published = new();

    public async Task<SyndicationResult> PublishAsync(BlogPost post, SyndicationOptions options, CancellationToken ct = default)
    {
        // Simulate API call delay
        await Task.Delay(500, ct);

        // Simulate successful publication
        var externalId = $"devto-{Guid.NewGuid():N}";
        var externalUrl = $"https://dev.to/techblog/{post.Slug}";

        _published[externalId] = (externalUrl, DateTime.UtcNow, 0, 0);

        return new SyndicationResult(
            Success: true,
            ExternalId: externalId,
            ExternalUrl: externalUrl,
            Error: null);
    }

    public async Task<SyndicationResult> UpdateAsync(string externalId, BlogPost post, CancellationToken ct = default)
    {
        await Task.Delay(300, ct);

        if (!_published.TryGetValue(externalId, out var existing))
        {
            return new SyndicationResult(false, null, null, "Post not found on DEV.to");
        }

        return new SyndicationResult(
            Success: true,
            ExternalId: externalId,
            ExternalUrl: existing.url,
            Error: null);
    }

    public async Task<bool> DeleteAsync(string externalId, CancellationToken ct = default)
    {
        await Task.Delay(200, ct);
        return _published.Remove(externalId);
    }

    public async Task<SyndicationStatus> GetStatusAsync(string externalId, CancellationToken ct = default)
    {
        await Task.Delay(100, ct);

        if (_published.TryGetValue(externalId, out var data))
        {
            // Simulate growing engagement
            var hoursSincePublish = (DateTime.UtcNow - data.publishedAt).TotalHours;
            var views = (int)(hoursSincePublish * Random.Shared.Next(5, 20));
            var reactions = (int)(views * 0.1);

            return new SyndicationStatus
            {
                IsLive = true,
                Views = views,
                Reactions = reactions,
                Comments = reactions / 3,
                PublishedAt = data.publishedAt,
                ExternalUrl = data.url
            };
        }

        return new SyndicationStatus { IsLive = false };
    }

    public async Task<ValidationResult> ValidateCredentialsAsync(CancellationToken ct = default)
    {
        await Task.Delay(100, ct);
        // Simulated - always valid for demo
        return new ValidationResult(true, null);
    }

    public SyndicationCapabilities GetCapabilities() => new()
    {
        SupportsDraft = true,
        SupportsScheduling = false,
        SupportsCanonicalUrl = true,
        SupportsUpdate = true,
        SupportsDelete = true,
        MaxTitleLength = 128,
        MaxTags = 4,
        RequiresImageUpload = false
    };
}
