namespace AspireDemo.Blog.Contracts;

/// <summary>
/// Plugin interface for blog data storage and retrieval.
/// Implementations provide different storage backends (InMemory, SQLite, PostgreSQL, etc.)
/// </summary>
public interface IBlogDataProvider
{
    /// <summary>Display name of the provider.</summary>
    string ProviderName { get; }

    /// <summary>Description of the provider's characteristics.</summary>
    string ProviderDescription { get; }

    /// <summary>Features supported by this provider.</summary>
    IReadOnlyList<string> Features { get; }

    // Posts
    Task<BlogPost> CreatePostAsync(CreatePostRequest request, CancellationToken ct = default);
    Task<BlogPost?> GetPostByIdAsync(Guid id, CancellationToken ct = default);
    Task<BlogPost?> GetPostBySlugAsync(string slug, CancellationToken ct = default);
    Task<PagedResult<BlogPost>> GetPostsAsync(PostQuery query, CancellationToken ct = default);
    Task<BlogPost> UpdatePostAsync(Guid id, UpdatePostRequest request, CancellationToken ct = default);
    Task<bool> DeletePostAsync(Guid id, CancellationToken ct = default);

    // Categories
    Task<List<Category>> GetCategoriesAsync(CancellationToken ct = default);
    Task<Category> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default);

    // Authors
    Task<Author?> GetAuthorAsync(Guid id, CancellationToken ct = default);
    Task<List<Author>> GetAuthorsAsync(CancellationToken ct = default);
    Task<Author> CreateAuthorAsync(CreateAuthorRequest request, CancellationToken ct = default);

    // Search
    Task<List<BlogPost>> SearchAsync(string query, int limit = 20, CancellationToken ct = default);

    // Statistics
    Task<BlogStats> GetStatsAsync(CancellationToken ct = default);

    // Syndication tracking
    Task UpdateSyndicationLinkAsync(Guid postId, string platform, string url, CancellationToken ct = default);
}
