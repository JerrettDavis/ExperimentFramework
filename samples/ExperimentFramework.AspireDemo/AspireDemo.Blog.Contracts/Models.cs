namespace AspireDemo.Blog.Contracts;

/// <summary>
/// Represents a blog post.
/// </summary>
public record BlogPost
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required string Content { get; init; }
    public string? Excerpt { get; init; }
    public string? FeaturedImage { get; init; }
    public PostStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? PublishedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public required Author Author { get; init; }
    public List<Category> Categories { get; init; } = [];
    public List<string> Tags { get; init; } = [];
    public int ViewCount { get; init; }
    public int ReadTimeMinutes { get; init; }
    public Dictionary<string, string> SyndicationLinks { get; init; } = [];
}

/// <summary>
/// Post publication status.
/// </summary>
public enum PostStatus
{
    Draft,
    Published,
    Scheduled,
    Archived
}

/// <summary>
/// Represents a blog author.
/// </summary>
public record Author
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
    public string? TwitterHandle { get; init; }
    public string? GitHubHandle { get; init; }
    public string? WebsiteUrl { get; init; }
    public DateTime JoinedAt { get; init; }
    public int PostCount { get; init; }
}

/// <summary>
/// Represents a post category.
/// </summary>
public record Category
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Description { get; init; }
    public string? Color { get; init; }
    public int PostCount { get; init; }
}

/// <summary>
/// Blog-wide statistics.
/// </summary>
public record BlogStats
{
    public int TotalPosts { get; init; }
    public int PublishedPosts { get; init; }
    public int DraftPosts { get; init; }
    public int TotalViews { get; init; }
    public int TotalAuthors { get; init; }
    public int TotalCategories { get; init; }
    public List<CategoryStats> CategoryBreakdown { get; init; } = [];
}

/// <summary>
/// Statistics for a single category.
/// </summary>
public record CategoryStats(string Name, int PostCount, int ViewCount);

/// <summary>
/// Paginated result wrapper.
/// </summary>
public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

/// <summary>
/// Query parameters for fetching posts.
/// </summary>
public record PostQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public PostStatus? Status { get; init; }
    public Guid? AuthorId { get; init; }
    public Guid? CategoryId { get; init; }
    public string? Tag { get; init; }
    public string? SearchTerm { get; init; }
    public PostSortBy SortBy { get; init; } = PostSortBy.PublishedAt;
    public bool Descending { get; init; } = true;
}

/// <summary>
/// Sort options for posts.
/// </summary>
public enum PostSortBy
{
    PublishedAt,
    CreatedAt,
    Title,
    ViewCount
}

/// <summary>
/// Request to create a new post.
/// </summary>
public record CreatePostRequest(
    string Title,
    string Content,
    Guid AuthorId,
    List<Guid>? CategoryIds = null,
    List<string>? Tags = null,
    string? FeaturedImage = null,
    PostStatus Status = PostStatus.Draft);

/// <summary>
/// Request to update an existing post.
/// </summary>
public record UpdatePostRequest(
    string? Title = null,
    string? Content = null,
    List<Guid>? CategoryIds = null,
    List<string>? Tags = null,
    string? FeaturedImage = null,
    PostStatus? Status = null);

/// <summary>
/// Request to create a new author.
/// </summary>
public record CreateAuthorRequest(
    string Name,
    string Email,
    string? Bio = null,
    string? AvatarUrl = null,
    string? TwitterHandle = null,
    string? GitHubHandle = null);

/// <summary>
/// Request to create a new category.
/// </summary>
public record CreateCategoryRequest(
    string Name,
    string? Description = null,
    string? Color = null);

/// <summary>
/// Authenticated blog user.
/// </summary>
public record BlogUser(
    Guid Id,
    string Username,
    string Email,
    string DisplayName,
    List<string> Roles);
