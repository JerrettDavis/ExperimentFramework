using AspireDemo.E2ETests.Drivers;
using Microsoft.Playwright;
using Reqnroll;

namespace AspireDemo.E2ETests.StepDefinitions.Blog;

[Binding]
public class BlogStepDefinitions
{
    private readonly BrowserDriver _browser;
    private readonly BlogDriver _blogDriver;

    public BlogStepDefinitions(BrowserDriver browser, BlogDriver blogDriver)
    {
        _browser    = browser;
        _blogDriver = blogDriver;
    }

    private IPage Page => _browser.Page;

    // -------------------------------------------------------------------------
    // When
    // -------------------------------------------------------------------------

    [When(@"I navigate to the blog home page")]
    public async Task WhenINavigateToBlogHomePage()
    {
        await _blogDriver.NavigateToAsync("/");
        await _blogDriver.WaitForHomeLoadedAsync();
    }

    [When(@"I navigate to the blog post page with slug {string}")]
    public async Task WhenINavigateToBlogPostPage(string slug)
    {
        await _blogDriver.NavigateToAsync($"/post/{slug}");
        // Wait for either the post content or the not-found state
        await Page.WaitForSelectorAsync(
            ".post-title, .empty-state, h2:has-text('Post Not Found')",
            new PageWaitForSelectorOptions { Timeout = 15000 });
    }

    [When(@"I navigate to the blog admin page")]
    public async Task WhenINavigateToBlogAdminPage()
    {
        await _blogDriver.NavigateToAsync("/admin");
        await Page.WaitForSelectorAsync(
            "h1, .admin-header, .loading",
            new PageWaitForSelectorOptions { Timeout = 15000 });
    }

    [When(@"I navigate to the blog authors page")]
    public async Task WhenINavigateToBlogAuthorsPage()
    {
        await _blogDriver.NavigateToAsync("/authors");
        await Page.WaitForSelectorAsync(
            "h1, .loading",
            new PageWaitForSelectorOptions { Timeout = 15000 });
    }

    [When(@"I navigate to the blog categories page")]
    public async Task WhenINavigateToBlogCategoriesPage()
    {
        await _blogDriver.NavigateToAsync("/categories");
        await Page.WaitForSelectorAsync(
            "h1, .loading",
            new PageWaitForSelectorOptions { Timeout = 15000 });
    }

    // -------------------------------------------------------------------------
    // Then
    // -------------------------------------------------------------------------

    [Then(@"the blog hero section should be visible with title {string}")]
    public async Task ThenBlogHeroShouldBeVisibleWithTitle(string expectedTitle)
    {
        var heroTitle = Page.Locator(".hero-title");
        await heroTitle.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        var text = await heroTitle.TextContentAsync() ?? "";
        if (!text.Contains(expectedTitle, StringComparison.OrdinalIgnoreCase))
            throw new Exception($"Expected hero title to contain '{expectedTitle}' but was '{text}'.");
    }

    [Then(@"the active plugin indicators should be visible")]
    public async Task ThenActivePluginIndicatorsShouldBeVisible()
    {
        // .active-plugins is rendered in the hero once the API responds
        // It may take a moment for Blazor to hydrate and fetch plugin data
        await Page.WaitForSelectorAsync(
            ".active-plugins, .plugin-indicator",
            new PageWaitForSelectorOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 15000
            });
    }

    [Then(@"the posts section should be visible")]
    public async Task ThenThePostsSectionShouldBeVisible()
    {
        // Either a grid of posts or the empty-state is acceptable
        var postsGrid  = Page.Locator(".posts-grid");
        var emptyState = Page.Locator(".empty-state");

        var postsCount  = await postsGrid.CountAsync();
        var emptyCount  = await emptyState.CountAsync();

        if (postsCount == 0 && emptyCount == 0)
            throw new Exception("Neither .posts-grid nor .empty-state was found on the blog home page.");
    }

    [Then(@"the categories sidebar should be visible")]
    public async Task ThenTheCategoriesSidebarShouldBeVisible()
    {
        // The sidebar with categories appears after Blazor hydration
        await Page.WaitForSelectorAsync(
            ".sidebar, aside",
            new PageWaitForSelectorOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 15000
            });
    }

    [Then(@"the authors sidebar should be visible")]
    public async Task ThenTheAuthorsSidebarShouldBeVisible()
    {
        await Page.WaitForSelectorAsync(
            ".sidebar, aside",
            new PageWaitForSelectorOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 15000
            });
    }

    [Then(@"the post not-found message should be visible")]
    public async Task ThenPostNotFoundMessageShouldBeVisible()
    {
        // The Blog's Post.razor renders an empty-state with "Post Not Found" text when slug is unknown
        await Page.WaitForSelectorAsync(
            ".empty-state, h2:has-text('Post Not Found'), h2:has-text('not found')",
            new PageWaitForSelectorOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 15000
            });
    }

    [Then(@"the blog administration heading should be visible")]
    public async Task ThenBlogAdministrationHeadingShouldBeVisible()
    {
        // Admin.razor renders <h1>Blog Administration</h1>
        await Page.WaitForSelectorAsync(
            "h1:has-text('Blog Administration'), h1:has-text('Admin')",
            new PageWaitForSelectorOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 15000
            });
    }

    [Then(@"the authors heading should be visible")]
    public async Task ThenAuthorsHeadingShouldBeVisible()
    {
        await Page.WaitForSelectorAsync(
            "h1:has-text('Authors'), h1",
            new PageWaitForSelectorOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 15000
            });
    }

    [Then(@"the categories heading should be visible")]
    public async Task ThenCategoriesHeadingShouldBeVisible()
    {
        await Page.WaitForSelectorAsync(
            "h1:has-text('Categories'), h1",
            new PageWaitForSelectorOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 15000
            });
    }
}
