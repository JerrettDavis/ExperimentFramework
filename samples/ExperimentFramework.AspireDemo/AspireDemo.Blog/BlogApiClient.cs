namespace AspireDemo.Blog;

public class BlogApiClient(HttpClient httpClient)
{
    // Blog Posts
    public async Task<List<BlogPostDto>> GetPostsAsync(string? status = null, string? category = null)
    {
        var url = "/api/blog/posts";
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(status)) queryParams.Add($"status={Uri.EscapeDataString(status)}");
        if (!string.IsNullOrEmpty(category)) queryParams.Add($"category={Uri.EscapeDataString(category)}");
        if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);

        return await httpClient.GetFromJsonAsync<List<BlogPostDto>>(url) ?? [];
    }

    public async Task<BlogPostDto?> GetPostAsync(string slug)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<BlogPostDto>($"/api/blog/posts/{slug}");
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<List<BlogPostDto>> SearchPostsAsync(string query)
    {
        return await httpClient.GetFromJsonAsync<List<BlogPostDto>>($"/api/blog/search?q={Uri.EscapeDataString(query)}") ?? [];
    }

    // Categories
    public async Task<List<BlogCategoryDto>> GetCategoriesAsync()
    {
        return await httpClient.GetFromJsonAsync<List<BlogCategoryDto>>("/api/blog/categories") ?? [];
    }

    // Authors
    public async Task<List<BlogAuthorDto>> GetAuthorsAsync()
    {
        return await httpClient.GetFromJsonAsync<List<BlogAuthorDto>>("/api/blog/authors") ?? [];
    }

    // Stats
    public async Task<BlogStatsDto?> GetStatsAsync()
    {
        return await httpClient.GetFromJsonAsync<BlogStatsDto>("/api/blog/stats");
    }

    // Plugin Management
    public async Task<BlogPluginsResponse?> GetPluginsAsync()
    {
        return await httpClient.GetFromJsonAsync<BlogPluginsResponse>("/api/blog/plugins");
    }

    public async Task<bool> ActivatePluginAsync(string pluginType, string alias)
    {
        var response = await httpClient.PostAsJsonAsync("/api/blog/plugins/activate", new { PluginType = pluginType, Alias = alias });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeactivatePluginAsync(string pluginType, string alias)
    {
        var response = await httpClient.PostAsJsonAsync("/api/blog/plugins/deactivate", new { PluginType = pluginType, Alias = alias });
        return response.IsSuccessStatusCode;
    }

    public async Task<BlogEditorConfigResponse?> GetEditorConfigAsync()
    {
        return await httpClient.GetFromJsonAsync<BlogEditorConfigResponse>("/api/blog/editor/config");
    }
}

// DTOs
public class BlogPostDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Content { get; set; } = "";
    public string? Excerpt { get; set; }
    public string? FeaturedImage { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public BlogAuthorDto? Author { get; set; }
    public List<BlogCategoryDto> Categories { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public int ViewCount { get; set; }
    public int ReadTimeMinutes { get; set; }
    public Dictionary<string, string> SyndicationLinks { get; set; } = [];
}

public class BlogAuthorDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? TwitterHandle { get; set; }
    public string? GitHubHandle { get; set; }
    public int PostCount { get; set; }
}

public class BlogCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string? Color { get; set; }
    public int PostCount { get; set; }
}

public class BlogStatsDto
{
    public int TotalPosts { get; set; }
    public int PublishedPosts { get; set; }
    public int DraftPosts { get; set; }
    public int TotalViews { get; set; }
    public int TotalAuthors { get; set; }
    public int TotalCategories { get; set; }
}

public class BlogPluginsResponse
{
    public PluginSection Data { get; set; } = new();
    public PluginSection Editor { get; set; } = new();
    public SyndicationPluginSection Syndication { get; set; } = new();
    public PluginSection Auth { get; set; } = new();
}

public class PluginSection
{
    public List<BlogPluginOption> Options { get; set; } = [];
    public string Active { get; set; } = "";
}

public class SyndicationPluginSection
{
    public List<BlogPluginOption> Options { get; set; } = [];
    public List<string> Active { get; set; } = [];
}

public class BlogPluginOption
{
    public string Alias { get; set; } = "";
    public string TypeName { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Features { get; set; } = [];
    public string? Color { get; set; }
}

public class BlogEditorConfigResponse
{
    public BlogPluginOption? Editor { get; set; }
    public EditorConfig? Config { get; set; }
}

public class EditorConfig
{
    public string Type { get; set; } = "";
    public string? ScriptUrl { get; set; }
    public string? StyleUrl { get; set; }
}
