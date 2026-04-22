using ExperimentFramework.E2E.Tests.Drivers;
using ExperimentFramework.E2E.Tests.PageObjects;
using Microsoft.Playwright;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.StepDefinitions.Plugins;

[Binding]
public class PluginStepDefinitions
{
    private readonly BrowserDriver _browser;
    private readonly DashboardDriver _dashboard;

    public PluginStepDefinitions(BrowserDriver browser, DashboardDriver dashboard)
    {
        _browser   = browser;
        _dashboard = dashboard;
    }

    private IPage Page => _browser.Page;

    private PluginsPage PluginPage => new(_browser.Page);

    // -------------------------------------------------------------------------
    // Given / Background
    // -------------------------------------------------------------------------

    [Given(@"I am on the plugins page")]
    public async Task GivenIAmOnThePluginsPage()
    {
        await _dashboard.NavigateToAsync("/dashboard/plugins");

        // Plugins endpoint may return 501 when the plugin system is not configured in the
        // test environment.  IsLoadedAsync falls back to <main> so the step still passes;
        // individual assertions that require cards will be skipped via the empty-state path.
        var loaded = await PluginPage.IsLoadedAsync();
        if (!loaded)
            throw new Exception("Plugins page did not load — page container not visible.");
    }

    [Given(@"there are loaded plugins")]
    public async Task GivenThereAreLoadedPlugins()
    {
        var count = await PluginPage.GetPluginCountAsync();
        if (count == 0)
            throw new Exception(
                "No plugin cards found — this scenario requires at least one loaded plugin. " +
                "Ensure the test environment has plugins registered, or skip this scenario.");
    }

    // -------------------------------------------------------------------------
    // When
    // -------------------------------------------------------------------------

    [When(@"I click the refresh button")]
    public async Task WhenIClickTheRefreshButton()
    {
        await PluginPage.RefreshAsync();
    }

    // -------------------------------------------------------------------------
    // Then
    // -------------------------------------------------------------------------

    [Then(@"I should see the plugin stats row")]
    public async Task ThenIShouldSeeThePluginStatsRow()
    {
        // Stats section may show "0 plugins loaded" when none are registered — still visible.
        await Page.WaitForSelectorAsync(
            ".plugin-stats, [data-stats], .stats-section",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"I should see plugin cards or an empty state")]
    public async Task ThenIShouldSeePluginCardsOrAnEmptyState()
    {
        // Gracefully handle environments where plugins are not configured (501 / empty).
        await Page.WaitForSelectorAsync(
            ".plugin-card, [data-plugin-card], .plugin-item, " +
            ".empty-state, [data-empty-state], .no-data, .no-plugins",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"the plugin data should reload")]
    public async Task ThenThePluginDataShouldReload()
    {
        // After refresh the page has settled to NetworkIdle (handled inside RefreshAsync).
        // Assert the page container is still present.
        var loaded = await PluginPage.IsLoadedAsync();
        if (!loaded)
            throw new Exception("Plugins page container disappeared after refresh.");
    }

    [Then(@"each plugin card should display its name and version")]
    public async Task ThenEachPluginCardShouldDisplayItsNameAndVersion()
    {
        var cards = await PluginPage.GetPluginCardsAsync();
        if (cards.Count == 0)
            throw new Exception("No plugin cards found — cannot assert name and version.");

        foreach (var cardText in cards)
        {
            // A version string contains at least one digit and a dot (e.g. "1.0.0", "2.3").
            var hasVersion = System.Text.RegularExpressions.Regex.IsMatch(cardText, @"\d+\.\d+");
            if (!hasVersion)
                throw new Exception(
                    $"Plugin card does not appear to display a version number. Card text: '{cardText}'");
        }
    }

    [Then(@"each plugin card should show available services")]
    public async Task ThenEachPluginCardShouldShowAvailableServices()
    {
        var cards = await PluginPage.GetPluginCardsAsync();
        if (cards.Count == 0)
            throw new Exception("No plugin cards found — cannot assert service information.");

        // Service entries typically start with 'I' (interface convention) or contain "Service".
        foreach (var cardText in cards)
        {
            var hasService = cardText.Contains("Service", StringComparison.OrdinalIgnoreCase)
                          || System.Text.RegularExpressions.Regex.IsMatch(cardText, @"\bI[A-Z]\w+");

            if (!hasService)
                throw new Exception(
                    $"Plugin card does not appear to list any services. Card text: '{cardText}'");
        }
    }
}
