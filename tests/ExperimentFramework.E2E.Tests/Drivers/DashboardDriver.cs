using ExperimentFramework.E2E.Tests.Support;
using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.Drivers;

/// <summary>
/// Dashboard-specific navigation and authentication helpers.
/// Wraps <see cref="BrowserDriver"/> with higher-level operations.
/// </summary>
public class DashboardDriver
{
    private readonly BrowserDriver _browserDriver;
    private readonly TestConfiguration _config;

    public DashboardDriver(BrowserDriver browserDriver, TestConfiguration config)
    {
        _browserDriver = browserDriver;
        _config        = config;
    }

    private IPage Page => _browserDriver.Page;

    // -------------------------------------------------------------------------
    // Navigation
    // -------------------------------------------------------------------------

    /// <summary>Navigates to <c>{BaseUrl}{path}</c> and waits until DOM content is loaded.</summary>
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

    /// <summary>Returns true when the current URL ends with <paramref name="expectedPath"/>.</summary>
    public async Task<bool> IsOnPageAsync(string expectedPath)
    {
        // Give the page a moment to settle after navigation
        await Page.WaitForURLAsync($"**{expectedPath}");
        return Page.Url.Contains(expectedPath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Waits for the main dashboard container to appear.</summary>
    public async Task WaitForDashboardLoadedAsync()
    {
        // Try the primary selector; fall back progressively for apps that differ.
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
                // Final fallback: any content inside the Blazor layout .page div
                await Page.WaitForSelectorAsync(".page, #app, body > div",
                    new PageWaitForSelectorOptions { Timeout = _config.DefaultTimeoutMs });
            }
        }
    }

    // -------------------------------------------------------------------------
    // Authentication
    // -------------------------------------------------------------------------

    /// <summary>Fills the login form with <paramref name="user"/> credentials and submits.</summary>
    public async Task LoginAsAsync(TestUser user)
    {
        await NavigateToAsync("/login");

        await Page.FillAsync("input[name='email'], input[type='email'], #email", user.Email);
        await Page.FillAsync("input[name='password'], input[type='password'], #password", user.Password);

        // Submit and wait for navigation to /dashboard (login redirects there).
        await Page.ClickAsync("button[type='submit'], input[type='submit'], .login-submit");
        await Page.WaitForURLAsync("**/dashboard**", new PageWaitForURLOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await WaitForDashboardLoadedAsync();
    }

    /// <summary>Logs in as the <see cref="TestUsers.Admin"/> user.</summary>
    public Task LoginAsAdminAsync() => LoginAsAsync(TestUsers.Admin);

    /// <summary>Logs in as the <see cref="TestUsers.Experimenter"/> user.</summary>
    public Task LoginAsExperimenterAsync() => LoginAsAsync(TestUsers.Experimenter);

    /// <summary>Logs in as the <see cref="TestUsers.Viewer"/> user.</summary>
    public Task LoginAsViewerAsync() => LoginAsAsync(TestUsers.Viewer);

    /// <summary>Logs in as the <see cref="TestUsers.Analyst"/> user.</summary>
    public Task LoginAsAnalystAsync() => LoginAsAsync(TestUsers.Analyst);

    /// <summary>Logs in as the user matching the given role name (case-insensitive).</summary>
    public Task LoginAsRoleAsync(string role) => LoginAsAsync(TestUsers.GetByRole(role));

    // -------------------------------------------------------------------------
    // Convenience
    // -------------------------------------------------------------------------

    /// <summary>Takes a screenshot and saves it to the <paramref name="filePath"/>.</summary>
    public async Task TakeScreenshotAsync(string filePath)
    {
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path     = filePath,
            FullPage = true
        });
    }
}
