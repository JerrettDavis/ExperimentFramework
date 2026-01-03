using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using AspireDemo.Blog.Contracts;

namespace AspireDemo.Blog.Plugins.Data;

/// <summary>
/// Fast in-memory data provider - data resets on restart.
/// Ideal for demos and development.
/// </summary>
public sealed partial class InMemoryDataProvider : IBlogDataProvider
{
    public string ProviderName => "In-Memory";
    public string ProviderDescription => "Fast volatile storage - data resets on restart. Ideal for demos.";
    public IReadOnlyList<string> Features => ["Fast reads", "No setup required", "Volatile"];

    private readonly ConcurrentDictionary<Guid, BlogPost> _posts = new();
    private readonly ConcurrentDictionary<Guid, Author> _authors = new();
    private readonly ConcurrentDictionary<Guid, Category> _categories = new();

    public Task<BlogPost> CreatePostAsync(CreatePostRequest request, CancellationToken ct = default)
    {
        var author = _authors.GetValueOrDefault(request.AuthorId)
            ?? throw new ArgumentException($"Author {request.AuthorId} not found");

        var categories = request.CategoryIds?
            .Select(id => _categories.GetValueOrDefault(id))
            .Where(c => c != null)
            .Cast<Category>()
            .ToList() ?? [];

        var post = new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Slug = GenerateSlug(request.Title),
            Content = request.Content,
            Excerpt = GenerateExcerpt(request.Content),
            FeaturedImage = request.FeaturedImage,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            PublishedAt = request.Status == PostStatus.Published ? DateTime.UtcNow : null,
            Author = author,
            Categories = categories,
            Tags = request.Tags ?? [],
            ReadTimeMinutes = CalculateReadTime(request.Content)
        };

        _posts[post.Id] = post;
        UpdateAuthorPostCount(author.Id);
        UpdateCategoryPostCounts();

        return Task.FromResult(post);
    }

    public Task<BlogPost?> GetPostByIdAsync(Guid id, CancellationToken ct = default)
    {
        _posts.TryGetValue(id, out var post);
        return Task.FromResult(post);
    }

    public Task<BlogPost?> GetPostBySlugAsync(string slug, CancellationToken ct = default)
    {
        var post = _posts.Values.FirstOrDefault(p => p.Slug == slug);
        if (post != null)
        {
            // Increment view count
            var updated = post with { ViewCount = post.ViewCount + 1 };
            _posts[post.Id] = updated;
            return Task.FromResult<BlogPost?>(updated);
        }
        return Task.FromResult<BlogPost?>(null);
    }

    public Task<PagedResult<BlogPost>> GetPostsAsync(PostQuery query, CancellationToken ct = default)
    {
        var posts = _posts.Values.AsEnumerable();

        // Filter by status
        if (query.Status.HasValue)
            posts = posts.Where(p => p.Status == query.Status.Value);

        // Filter by author
        if (query.AuthorId.HasValue)
            posts = posts.Where(p => p.Author.Id == query.AuthorId.Value);

        // Filter by category
        if (query.CategoryId.HasValue)
            posts = posts.Where(p => p.Categories.Any(c => c.Id == query.CategoryId.Value));

        // Filter by tag
        if (!string.IsNullOrEmpty(query.Tag))
            posts = posts.Where(p => p.Tags.Contains(query.Tag, StringComparer.OrdinalIgnoreCase));

        // Search
        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLowerInvariant();
            posts = posts.Where(p =>
                p.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Content.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        // Sort
        posts = query.SortBy switch
        {
            PostSortBy.CreatedAt => query.Descending ? posts.OrderByDescending(p => p.CreatedAt) : posts.OrderBy(p => p.CreatedAt),
            PostSortBy.Title => query.Descending ? posts.OrderByDescending(p => p.Title) : posts.OrderBy(p => p.Title),
            PostSortBy.ViewCount => query.Descending ? posts.OrderByDescending(p => p.ViewCount) : posts.OrderBy(p => p.ViewCount),
            _ => query.Descending ? posts.OrderByDescending(p => p.PublishedAt ?? p.CreatedAt) : posts.OrderBy(p => p.PublishedAt ?? p.CreatedAt)
        };

        var totalCount = posts.Count();
        var items = posts
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return Task.FromResult(new PagedResult<BlogPost>(items, totalCount, query.Page, query.PageSize));
    }

    public Task<BlogPost> UpdatePostAsync(Guid id, UpdatePostRequest request, CancellationToken ct = default)
    {
        if (!_posts.TryGetValue(id, out var existing))
            throw new ArgumentException($"Post {id} not found");

        var categories = request.CategoryIds != null
            ? request.CategoryIds
                .Select(cid => _categories.GetValueOrDefault(cid))
                .Where(c => c != null)
                .Cast<Category>()
                .ToList()
            : existing.Categories;

        var updated = existing with
        {
            Title = request.Title ?? existing.Title,
            Slug = request.Title != null ? GenerateSlug(request.Title) : existing.Slug,
            Content = request.Content ?? existing.Content,
            Excerpt = request.Content != null ? GenerateExcerpt(request.Content) : existing.Excerpt,
            FeaturedImage = request.FeaturedImage ?? existing.FeaturedImage,
            Status = request.Status ?? existing.Status,
            UpdatedAt = DateTime.UtcNow,
            PublishedAt = request.Status == PostStatus.Published && existing.PublishedAt == null
                ? DateTime.UtcNow
                : existing.PublishedAt,
            Categories = categories,
            Tags = request.Tags ?? existing.Tags,
            ReadTimeMinutes = request.Content != null ? CalculateReadTime(request.Content) : existing.ReadTimeMinutes
        };

        _posts[id] = updated;
        UpdateCategoryPostCounts();
        return Task.FromResult(updated);
    }

    public Task<bool> DeletePostAsync(Guid id, CancellationToken ct = default)
    {
        var removed = _posts.TryRemove(id, out var post);
        if (removed && post != null)
        {
            UpdateAuthorPostCount(post.Author.Id);
            UpdateCategoryPostCounts();
        }
        return Task.FromResult(removed);
    }

    public Task<List<Category>> GetCategoriesAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_categories.Values.OrderBy(c => c.Name).ToList());
    }

    public Task<Category> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = GenerateSlug(request.Name),
            Description = request.Description,
            Color = request.Color ?? GenerateColor()
        };

        _categories[category.Id] = category;
        return Task.FromResult(category);
    }

    public Task<Author?> GetAuthorAsync(Guid id, CancellationToken ct = default)
    {
        _authors.TryGetValue(id, out var author);
        return Task.FromResult(author);
    }

    public Task<List<Author>> GetAuthorsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_authors.Values.OrderBy(a => a.Name).ToList());
    }

    public Task<Author> CreateAuthorAsync(CreateAuthorRequest request, CancellationToken ct = default)
    {
        var author = new Author
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Bio = request.Bio,
            AvatarUrl = request.AvatarUrl ?? $"https://api.dicebear.com/7.x/avataaars/svg?seed={request.Name}",
            TwitterHandle = request.TwitterHandle,
            GitHubHandle = request.GitHubHandle,
            JoinedAt = DateTime.UtcNow
        };

        _authors[author.Id] = author;
        return Task.FromResult(author);
    }

    public Task<List<BlogPost>> SearchAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        var term = query.ToLowerInvariant();
        var results = _posts.Values
            .Where(p => p.Status == PostStatus.Published)
            .Where(p =>
                p.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Content.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Tags.Any(t => t.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(p => p.PublishedAt)
            .Take(limit)
            .ToList();

        return Task.FromResult(results);
    }

    public Task<BlogStats> GetStatsAsync(CancellationToken ct = default)
    {
        var posts = _posts.Values.ToList();
        var stats = new BlogStats
        {
            TotalPosts = posts.Count,
            PublishedPosts = posts.Count(p => p.Status == PostStatus.Published),
            DraftPosts = posts.Count(p => p.Status == PostStatus.Draft),
            TotalViews = posts.Sum(p => p.ViewCount),
            TotalAuthors = _authors.Count,
            TotalCategories = _categories.Count,
            CategoryBreakdown = _categories.Values
                .Select(c => new CategoryStats(
                    c.Name,
                    posts.Count(p => p.Categories.Any(pc => pc.Id == c.Id)),
                    posts.Where(p => p.Categories.Any(pc => pc.Id == c.Id)).Sum(p => p.ViewCount)))
                .ToList()
        };

        return Task.FromResult(stats);
    }

    public Task UpdateSyndicationLinkAsync(Guid postId, string platform, string url, CancellationToken ct = default)
    {
        if (_posts.TryGetValue(postId, out var post))
        {
            var links = new Dictionary<string, string>(post.SyndicationLinks) { [platform] = url };
            _posts[postId] = post with { SyndicationLinks = links };
        }
        return Task.CompletedTask;
    }

    private void UpdateAuthorPostCount(Guid authorId)
    {
        if (_authors.TryGetValue(authorId, out var author))
        {
            var count = _posts.Values.Count(p => p.Author.Id == authorId);
            _authors[authorId] = author with { PostCount = count };
        }
    }

    private void UpdateCategoryPostCounts()
    {
        foreach (var category in _categories.Values)
        {
            var count = _posts.Values.Count(p => p.Categories.Any(c => c.Id == category.Id));
            _categories[category.Id] = category with { PostCount = count };
        }
    }

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant();
        slug = SlugRegex().Replace(slug, "-");
        slug = MultiDashRegex().Replace(slug, "-");
        return slug.Trim('-');
    }

    private static string GenerateExcerpt(string content, int maxLength = 200)
    {
        // Strip markdown/HTML and take first N characters
        var plain = HtmlTagRegex().Replace(content, "");
        plain = MarkdownLinkRegex().Replace(plain, "$1");
        plain = MarkdownHeaderRegex().Replace(plain, "");

        if (plain.Length <= maxLength)
            return plain.Trim();

        var excerpt = plain[..maxLength];
        var lastSpace = excerpt.LastIndexOf(' ');
        if (lastSpace > 0)
            excerpt = excerpt[..lastSpace];

        return excerpt.Trim() + "...";
    }

    private static int CalculateReadTime(string content)
    {
        var words = content.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Ceiling(words / 200.0)); // ~200 words per minute
    }

    private static string GenerateColor()
    {
        var colors = new[] { "#3b82f6", "#10b981", "#f59e0b", "#ef4444", "#8b5cf6", "#ec4899", "#06b6d4" };
        return colors[Random.Shared.Next(colors.Length)];
    }

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex SlugRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultiDashRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\([^\)]+\)")]
    private static partial Regex MarkdownLinkRegex();

    [GeneratedRegex(@"^#+\s*", RegexOptions.Multiline)]
    private static partial Regex MarkdownHeaderRegex();
}
