using AspireDemo.E2ETests.Support;
using Microsoft.Playwright;

namespace AspireDemo.E2ETests.Drivers;

/// <summary>
/// Navigation helpers for the AspireDemo Blog service
/// (https://localhost:7120 by default).
/// The Blog is a separate Blazor app served by <c>AspireDemo.Blog</c>.
/// </summary>
public class BlogDriver
{
    private readonly BrowserDriver _browserDriver;
    private readonly TestConfiguration _config;

    public BlogDriver(BrowserDriver browserDriver, TestConfiguration config)
    {
        _browserDriver = browserDriver;
        _config        = config;
    }

    private IPage Page => _browserDriver.Page;

    // -------------------------------------------------------------------------
    // Navigation — Blog service
    // -------------------------------------------------------------------------

    /// <summary>Navigates to <c>{BlogBaseUrl}{path}</c> and waits until the page is loaded.</summary>
    public async Task NavigateToAsync(string path)
    {
        var url = $"{_config.BlogBaseUrl.TrimEnd('/')}{path}";
        await Page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.Load });
    }

    /// <summary>
    /// Waits until the blog home page has loaded its post list (or the empty-state element).
    /// The page renders server-side first then switches to interactive Blazor, so we wait
    /// for either the posts grid or the empty/loading-complete indicator.
    /// </summary>
    public async Task WaitForHomeLoadedAsync()
    {
        // Wait for either posts or the empty state — both indicate loading finished
        await Page.WaitForSelectorAsync(
            ".posts-grid, .empty-state, .hero-title",
            new PageWaitForSelectorOptions { Timeout = _config.DefaultTimeoutMs });
    }

    /// <summary>Returns true when the current URL contains <paramref name="expectedPath"/>.</summary>
    public bool IsOnPage(string expectedPath) =>
        Page.Url.Contains(expectedPath, StringComparison.OrdinalIgnoreCase);

    /// <summary>Takes a full-page screenshot and saves it to <paramref name="filePath"/>.</summary>
    public async Task TakeScreenshotAsync(string filePath)
    {
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path     = filePath,
            FullPage = true
        });
    }
}
