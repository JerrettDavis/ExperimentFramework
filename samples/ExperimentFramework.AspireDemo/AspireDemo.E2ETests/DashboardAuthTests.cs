using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace AspireDemo.E2ETests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class DashboardAuthTests : PageTest
{
    private const string BaseUrl = "https://localhost:7201";
    private const string AdminEmail = "admin@experimentdemo.com";
    private const string AdminPassword = "Admin123!";

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions()
        {
            IgnoreHTTPSErrors = true, // For local development with self-signed cert
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        };
    }

    [SetUp]
    public async Task Setup()
    {
        // Set console message handler to capture errors
        Page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                TestContext.WriteLine($"BROWSER ERROR: {msg.Text}");
            }
            else if (msg.Type == "warning")
            {
                TestContext.WriteLine($"BROWSER WARNING: {msg.Text}");
            }
            else
            {
                TestContext.WriteLine($"BROWSER LOG: {msg.Text}");
            }
        };

        Page.PageError += (_, exception) =>
        {
            TestContext.WriteLine($"PAGE ERROR: {exception}");
        };
    }

    [Test]
    public async Task Dashboard_RequiresAuthentication()
    {
        // Navigate to dashboard without being logged in
        await Page.GotoAsync($"{BaseUrl}/dashboard");

        // Wait a bit to see what happens
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Take screenshot for debugging
        await Page.ScreenshotAsync(new() { Path = "dashboard_unauthenticated.png", FullPage = true });

        // Check URL - should either redirect to login or show not authorized
        var currentUrl = Page.Url;
        TestContext.WriteLine($"Current URL after navigating to /dashboard: {currentUrl}");

        // Check page content
        var pageContent = await Page.ContentAsync();
        TestContext.WriteLine($"Page HTML length: {pageContent.Length}");

        // Look for authorization-related text
        var bodyText = await Page.Locator("body").TextContentAsync();
        TestContext.WriteLine($"Body text: {bodyText}");

        Assert.That(currentUrl.Contains("/Account/Login") ||
                    bodyText?.Contains("Not Authorized") == true ||
                    bodyText?.Contains("Sign In") == true,
                    "Unauthenticated user should be redirected to login or see not authorized message");
    }

    [Test]
    public async Task LoginFlow_WorksCorrectly()
    {
        // Navigate to login page
        await Page.GotoAsync($"{BaseUrl}/Account/Login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Take screenshot
        await Page.ScreenshotAsync(new() { Path = "login_page.png", FullPage = true });

        // Fill in login form
        await Page.FillAsync("input[name='email']", AdminEmail);
        await Page.FillAsync("input[name='password']", AdminPassword);

        // Submit form
        await Page.ClickAsync("button[type='submit']");

        // Wait for navigation
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Take screenshot after login
        await Page.ScreenshotAsync(new() { Path = "after_login.png", FullPage = true });

        var currentUrl = Page.Url;
        TestContext.WriteLine($"URL after login: {currentUrl}");

        // Should be redirected to dashboard
        Assert.That(currentUrl, Does.Contain("/dashboard"), "After login, should redirect to dashboard");
    }

    [Test]
    public async Task Dashboard_LoadsForAuthenticatedUser()
    {
        // First, log in
        await Page.GotoAsync($"{BaseUrl}/Account/Login");
        await Page.FillAsync("input[name='email']", AdminEmail);
        await Page.FillAsync("input[name='password']", AdminPassword);
        await Page.ClickAsync("button[type='submit']");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        TestContext.WriteLine($"After login, URL: {Page.Url}");

        // Take screenshot of initial dashboard load
        await Page.ScreenshotAsync(new() { Path = "dashboard_initial_load.png", FullPage = true });

        // Wait for dashboard to load (give it 10 seconds max)
        try
        {
            // Try to wait for some expected dashboard content
            await Page.WaitForSelectorAsync("text=Experiment Dashboard", new() { Timeout = 10000 });
            TestContext.WriteLine("Found 'Experiment Dashboard' text");
        }
        catch (TimeoutException)
        {
            TestContext.WriteLine("TIMEOUT: Could not find 'Experiment Dashboard' text within 10 seconds");

            // Get all text content
            var allText = await Page.Locator("body").TextContentAsync();
            TestContext.WriteLine($"All page text: {allText}");

            // Get computed background color to check if it's blue
            var bgColor = await Page.EvaluateAsync<string>(@"
                window.getComputedStyle(document.body).backgroundColor
            ");
            TestContext.WriteLine($"Body background color: {bgColor}");

            // Check if page is stuck in authorization check
            var authorizingText = await Page.Locator("text=Checking authorization").CountAsync();
            if (authorizingText > 0)
            {
                TestContext.WriteLine("ERROR: Page stuck on 'Checking authorization...'");
            }

            var notAuthorizedText = await Page.Locator("text=Not Authorized").CountAsync();
            if (notAuthorizedText > 0)
            {
                TestContext.WriteLine("ERROR: User authenticated but shown 'Not Authorized'");
            }

            // Take final screenshot
            await Page.ScreenshotAsync(new() { Path = "dashboard_failed_load.png", FullPage = true });

            Assert.Fail("Dashboard did not load within 10 seconds");
        }

        // If we got here, check that actual content is visible
        var dashboardContent = await Page.Locator(".home-container, .feature-card, .hero").CountAsync();
        TestContext.WriteLine($"Found {dashboardContent} dashboard content elements");

        // Take final success screenshot
        await Page.ScreenshotAsync(new() { Path = "dashboard_loaded_success.png", FullPage = true });

        Assert.That(dashboardContent, Is.GreaterThan(0), "Dashboard should have visible content");
    }

    [Test]
    public async Task Dashboard_Navigation_Works()
    {
        // Log in first
        await Page.GotoAsync($"{BaseUrl}/Account/Login");
        await Page.FillAsync("input[name='email']", AdminEmail);
        await Page.FillAsync("input[name='password']", AdminPassword);
        await Page.ClickAsync("button[type='submit']");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Try to navigate to Experiments page
        await Page.GotoAsync($"{BaseUrl}/dashboard/experiments");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        TestContext.WriteLine($"Experiments page URL: {Page.Url}");

        // Take screenshot
        await Page.ScreenshotAsync(new() { Path = "experiments_page.png", FullPage = true });

        // Check if page loaded
        try
        {
            await Page.WaitForSelectorAsync("text=Experiments, text=No experiments", new() { Timeout = 10000 });
            TestContext.WriteLine("Experiments page loaded successfully");
        }
        catch (TimeoutException)
        {
            var pageText = await Page.Locator("body").TextContentAsync();
            TestContext.WriteLine($"Experiments page text: {pageText}");
            await Page.ScreenshotAsync(new() { Path = "experiments_page_failed.png", FullPage = true });
            Assert.Fail("Experiments page did not load");
        }
    }
}
