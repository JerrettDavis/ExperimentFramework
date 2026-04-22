using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the shared sidebar navigation component present on all dashboard pages.
/// </summary>
public class NavMenuComponent
{
    private readonly IPage _page;

    // The sidebar / nav container
    private ILocator NavContainer   => _page.Locator("nav, .sidebar, .nav-menu, [role='navigation']").First;
    private ILocator AllNavLinks    => NavContainer.Locator("a, button[role='link'], [role='menuitem']");
    private ILocator ActiveNavItem  => NavContainer.Locator(".active, [aria-current='page'], .nav-link.active, .nav-item.active");

    public NavMenuComponent(IPage page)
    {
        _page = page;
    }

    // -----------------------------------------------------------------------
    // Individual navigation helpers
    // -----------------------------------------------------------------------

    public Task GoToExperimentsAsync()  => ClickNavLinkAsync("Experiments");
    public Task GoToAnalyticsAsync()    => ClickNavLinkAsync("Analytics");
    public Task GoToGovernanceAsync()   => ClickNavLinkAsync("Governance");
    public Task GoToTargetingAsync()    => ClickNavLinkAsync("Targeting");
    public Task GoToRolloutAsync()      => ClickNavLinkAsync("Rollout");
    public Task GoToHypothesisAsync()   => ClickNavLinkAsync("Hypothesis");
    public Task GoToPluginsAsync()      => ClickNavLinkAsync("Plugins");
    public Task GoToConfigurationAsync() => ClickNavLinkAsync("Configuration");
    public Task GoToDslEditorAsync()    => ClickNavLinkAsync("DSL Editor");
    public Task GoToHomeAsync()         => ClickNavLinkAsync("Home");

    // -----------------------------------------------------------------------
    // Inspection
    // -----------------------------------------------------------------------

    /// <summary>Returns the text label of the currently highlighted nav item, or null if none.</summary>
    public async Task<string?> GetActiveNavItemAsync()
    {
        var count = await ActiveNavItem.CountAsync();
        if (count == 0)
            return null;

        return (await ActiveNavItem.First.TextContentAsync())?.Trim();
    }

    /// <summary>Returns the text labels of all nav items in the sidebar.</summary>
    public async Task<IReadOnlyList<string>> GetAllNavItemsAsync()
    {
        var count = await AllNavLinks.CountAsync();
        var labels = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var text = await AllNavLinks.Nth(i).TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
                labels.Add(text.Trim());
        }

        return labels;
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private async Task ClickNavLinkAsync(string label)
    {
        // Try an exact-text match inside the nav, then fall back to contains
        var link = NavContainer
            .Locator($"a, button, [role='menuitem']")
            .Filter(new LocatorFilterOptions { HasText = label });

        await link.First.ClickAsync();
    }
}
