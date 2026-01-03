using AspireDemo.Blog.Contracts;

namespace AspireDemo.Blog.Plugins.Syndication;

/// <summary>
/// Syndicates content to Medium.
/// Simulated for demo purposes.
/// </summary>
public sealed class MediumSyndicator : IBlogSyndicationProvider
{
    public string PlatformName => "Medium";
    public string PlatformUrl => "https://medium.com";
    public string PlatformIcon => "medium";
    public string PlatformColor => "#000000";

    private readonly Dictionary<string, (string url, DateTime publishedAt, int views, int claps)> _published = new();

    public async Task<SyndicationResult> PublishAsync(BlogPost post, SyndicationOptions options, CancellationToken ct = default)
    {
        await Task.Delay(700, ct);

        var externalId = $"medium-{Guid.NewGuid():N}";
        var externalUrl = $"https://medium.com/@techblog/{post.Slug}-{externalId[7..15]}";

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

        // Medium has limited update support
        if (!_published.TryGetValue(externalId, out var existing))
        {
            return new SyndicationResult(false, null, null, "Post not found on Medium");
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
        // Medium doesn't allow API deletion - simulate as always failing
        return false;
    }

    public async Task<SyndicationStatus> GetStatusAsync(string externalId, CancellationToken ct = default)
    {
        await Task.Delay(100, ct);

        if (_published.TryGetValue(externalId, out var data))
        {
            var hoursSincePublish = (DateTime.UtcNow - data.publishedAt).TotalHours;
            var views = (int)(hoursSincePublish * Random.Shared.Next(15, 50));
            var claps = (int)(views * 0.2); // Medium uses claps instead of reactions

            return new SyndicationStatus
            {
                IsLive = true,
                Views = views,
                Reactions = claps,
                Comments = claps / 10,
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
        SupportsDraft = false, // Medium publishes immediately via API
        SupportsScheduling = false,
        SupportsCanonicalUrl = true,
        SupportsUpdate = true,
        SupportsDelete = false, // Medium API doesn't support delete
        MaxTitleLength = 100,
        MaxTags = 5,
        RequiresImageUpload = true
    };
}
