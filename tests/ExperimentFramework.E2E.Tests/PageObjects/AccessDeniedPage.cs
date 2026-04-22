using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the access-denied page at <c>/Account/AccessDenied</c>.
/// </summary>
public class AccessDeniedPage
{
    private readonly IPage _page;

    private ILocator PageContainer      => _page.Locator(".access-denied, [data-page='access-denied'], main");
    private ILocator DashboardLink      => _page.Locator("a:has-text('Dashboard'), a[href*='/dashboard'], button:has-text('Go to Dashboard')");
    private ILocator SignOutLink        => _page.Locator("a:has-text('Sign Out'), a[href*='logout' i], a[href*='signout' i], button:has-text('Sign Out')");

    public AccessDeniedPage(IPage page)
    {
        _page = page;
    }

    /// <summary>Verifies the access-denied page is showing.</summary>
    public async Task<bool> IsLoadedAsync()
    {
        try
        {
            // The page must contain an "access denied" heading or the container
            var heading = _page.Locator("h1, h2").Filter(new LocatorFilterOptions { HasText = "Access Denied" });
            var headingCount = await heading.CountAsync();

            if (headingCount > 0)
                return true;

            await PageContainer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    /// <summary>Clicks the link / button that navigates back to the dashboard.</summary>
    public Task GoToDashboardAsync() =>
        DashboardLink.First.ClickAsync();

    /// <summary>Clicks the Sign Out link / button.</summary>
    public Task SignOutAsync() =>
        SignOutLink.First.ClickAsync();
}
