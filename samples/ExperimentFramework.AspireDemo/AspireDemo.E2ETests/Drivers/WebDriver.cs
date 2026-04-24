using AspireDemo.E2ETests.Support;
using Microsoft.Playwright;

namespace AspireDemo.E2ETests.Drivers;

/// <summary>
/// Navigation and authentication helpers for the AspireDemo web frontend
/// (https://localhost:7201 by default).
/// Wraps <see cref="BrowserDriver"/> with higher-level operations.
/// </summary>
public class WebDriver
{
    private readonly BrowserDriver _browserDriver;
    private readonly TestConfiguration _config;

    public WebDriver(BrowserDriver browserDriver, TestConfiguration config)
    {
        _browserDriver = browserDriver;
        _config        = config;
    }

    private IPage Page => _browserDriver.Page;

    // -------------------------------------------------------------------------
    // Navigation — Web frontend
    // -------------------------------------------------------------------------

    /// <summary>Navigates to <c>{BaseUrl}{path}</c> and waits until the page is loaded.</summary>
    public async Task NavigateToAsync(string path)
    {
        var url = $"{_config.BaseUrl.TrimEnd('/')}{path}";
        await Page.GotoAsync(url, new PageGotoOptions
        {
            // Use Load (not NetworkIdle) so Blazor's persistent SignalR connection
            // doesn't block navigation from completing.
            WaitUntil = WaitUntilState.Load
        });
    }

    /// <summary>Returns true when the current URL contains <paramref name="expectedPath"/>.</summary>
    public async Task<bool> IsOnPageAsync(string expectedPath)
    {
        await Page.WaitForURLAsync($"**{expectedPath}");
        return Page.Url.Contains(expectedPath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Waits for the main dashboard container to appear.</summary>
    public async Task WaitForDashboardLoadedAsync()
    {
        try
        {
            await Page.WaitForSelectorAsync(".home-container",
                new PageWaitForSelectorOptions { Timeout = _config.DefaultTimeoutMs });
        }
        catch (TimeoutException)
        {
            try
            {
                await Page.WaitForSelectorAsync("main, [role='main']",
                    new PageWaitForSelectorOptions { Timeout = _config.DefaultTimeoutMs });
            }
            catch (TimeoutException)
            {
                await Page.WaitForSelectorAsync(".page, #app, body > div",
                    new PageWaitForSelectorOptions { Timeout = _config.DefaultTimeoutMs });
            }
        }
    }

    // -------------------------------------------------------------------------
    // Authentication
    // -------------------------------------------------------------------------

    /// <summary>
    /// Navigates to /Account/Login, fills credentials, submits, and waits for /dashboard.
    /// The login page is a Razor Page at /Account/Login (not /login).
    /// </summary>
    public async Task LoginAsAsync(TestUser user)
    {
        await NavigateToAsync("/Account/Login");

        await Page.FillAsync("input[name='Email'], input[name='email'], input[type='email']", user.Email);
        await Page.FillAsync("input[name='Password'], input[name='password'], input[type='password']", user.Password);

        // Submit and wait for redirect to /dashboard
        await Page.ClickAsync("button[type='submit'], input[type='submit']");
        await Page.WaitForURLAsync("**/dashboard**", new PageWaitForURLOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await WaitForDashboardLoadedAsync();
    }

    public Task LoginAsAdminAsync()        => LoginAsAsync(TestUsers.Admin);
    public Task LoginAsExperimenterAsync() => LoginAsAsync(TestUsers.Experimenter);
    public Task LoginAsViewerAsync()       => LoginAsAsync(TestUsers.Viewer);
    public Task LoginAsAnalystAsync()      => LoginAsAsync(TestUsers.Analyst);
    public Task LoginAsRoleAsync(string role) => LoginAsAsync(TestUsers.GetByRole(role));

    // -------------------------------------------------------------------------
    // Convenience
    // -------------------------------------------------------------------------

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
