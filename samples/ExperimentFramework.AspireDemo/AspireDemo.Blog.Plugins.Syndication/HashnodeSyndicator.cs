using AspireDemo.Blog.Contracts;

namespace AspireDemo.Blog.Plugins.Syndication;

/// <summary>
/// Syndicates content to Hashnode.
/// Simulated for demo purposes.
/// </summary>
public sealed class HashnodeSyndicator : IBlogSyndicationProvider
{
    public string PlatformName => "Hashnode";
    public string PlatformUrl => "https://hashnode.com";
    public string PlatformIcon => "hashnode";
    public string PlatformColor => "#2962ff";

    private readonly Dictionary<string, (string url, DateTime publishedAt, int views, int reactions)> _published = new();

    public async Task<SyndicationResult> PublishAsync(BlogPost post, SyndicationOptions options, CancellationToken ct = default)
    {
        await Task.Delay(600, ct);

        var externalId = $"hashnode-{Guid.NewGuid():N}";
        var externalUrl = $"https://techblog.hashnode.dev/{post.Slug}";

        _published[externalId] = (externalUrl, DateTime.UtcNow, 0, 0);

        return new SyndicationResult(
            Success: true,
            ExternalId: externalId,
            ExternalUrl: externalUrl,
            Error: null);
    }

    public async Task<SyndicationResult> UpdateAsync(string externalId, BlogPost post, CancellationToken ct = default)
    {
        await Task.Delay(400, ct);

        if (!_published.TryGetValue(externalId, out var existing))
        {
            return new SyndicationResult(false, null, null, "Post not found on Hashnode");
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
            var hoursSincePublish = (DateTime.UtcNow - data.publishedAt).TotalHours;
            var views = (int)(hoursSincePublish * Random.Shared.Next(10, 30));
            var reactions = (int)(views * 0.15);

            return new SyndicationStatus
            {
                IsLive = true,
                Views = views,
                Reactions = reactions,
                Comments = reactions / 4,
                PublishedAt = data.publishedAt,
                ExternalUrl = data.url
            };
        }

        return new SyndicationStatus { IsLive = false };
    }

    public async Task<ValidationResult> ValidateCredentialsAsync(CancellationToken ct = default)
    {
        await Task.Delay(100, ct);
        return new ValidationResult(true, null);
    }

    public SyndicationCapabilities GetCapabilities() => new()
    {
        SupportsDraft = true,
        SupportsScheduling = true,
        SupportsCanonicalUrl = true,
        SupportsUpdate = true,
        SupportsDelete = true,
        MaxTitleLength = 250,
        MaxTags = 5,
        RequiresImageUpload = false
    };
}
