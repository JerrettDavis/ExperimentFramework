using ExperimentFramework.E2E.Tests.Drivers;
using ExperimentFramework.E2E.Tests.PageObjects;
using Microsoft.Playwright;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.StepDefinitions.Experiments;

[Binding]
public class ExperimentStepDefinitions
{
    private readonly BrowserDriver _browser;
    private readonly DashboardDriver _dashboard;
    private readonly ExperimentsPage _experimentsPage;

    // Tracks the first expanded experiment name across steps within a scenario
    private string? _firstExperimentName;

    // Tracks kill-switch state before toggle so we can assert it changed
    private string? _killSwitchStateBefore;

    public ExperimentStepDefinitions(BrowserDriver browser, DashboardDriver dashboard)
    {
        _browser         = browser;
        _dashboard       = dashboard;
        _experimentsPage = new ExperimentsPage(browser.Page);
    }

    private IPage Page => _browser.Page;

    // -------------------------------------------------------------------------
    // Background
    // -------------------------------------------------------------------------

    [Given(@"I am on the experiments page")]
    public async Task GivenIAmOnTheExperimentsPage()
    {
        await _dashboard.NavigateToAsync("/dashboard/experiments");
        await _experimentsPage.WaitForExperimentsLoadedAsync();
    }

    // -------------------------------------------------------------------------
    // Slow-connection scenario setup
    // -------------------------------------------------------------------------

    [Given(@"I am on a slow connection")]
    public async Task GivenIAmOnASlowConnection()
    {
        // Throttle all requests to simulate a slow network so skeleton loaders
        // have time to appear before content resolves.
        await Page.RouteAsync("**/*", async route =>
        {
            await Task.Delay(1500);
            await route.ContinueAsync();
        });
    }

    // -------------------------------------------------------------------------
    // When
    // -------------------------------------------------------------------------

    [When(@"I navigate to the experiments page")]
    public async Task WhenINavigateToTheExperimentsPage()
    {
        await _dashboard.NavigateToAsync("/dashboard/experiments");
    }

    [When(@"I filter experiments by category {string}")]
    public async Task WhenIFilterExperimentsByCategory(string category)
    {
        await _experimentsPage.FilterByCategoryAsync(category);
    }

    [When(@"I search for {string}")]
    public async Task WhenISearchFor(string query)
    {
        await _experimentsPage.SearchAsync(query);
    }

    [When(@"I clear the search")]
    public async Task WhenIClearTheSearch()
    {
        var searchInput = Page.Locator(
            "input[type='search'], input[placeholder*='search' i], input[name*='search' i]");
        await searchInput.ClearAsync();
        await searchInput.PressAsync("Enter");
    }

    [When(@"I expand the first experiment")]
    public async Task WhenIExpandTheFirstExperiment()
    {
        // Capture the name of the first item so subsequent steps can reference it
        var firstItem = Page.Locator(
            ".experiment-item, .experiment-row, [data-experiment]").First;

        _firstExperimentName = (await firstItem.TextContentAsync() ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?.Trim() ?? string.Empty;

        var toggle = firstItem.Locator(
            ".expand-toggle, .collapse-toggle, [aria-expanded='false'], button.toggle");
        await toggle.First.ClickAsync();
    }

    [When(@"I collapse the first experiment")]
    public async Task WhenICollapseTheFirstExperiment()
    {
        if (_firstExperimentName is null)
            throw new InvalidOperationException("No experiment has been expanded yet. Call 'I expand the first experiment' first.");

        await _experimentsPage.CollapseExperimentAsync(_firstExperimentName);
    }

    [When(@"I toggle the kill switch")]
    public async Task WhenIToggleTheKillSwitch()
    {
        if (_firstExperimentName is null)
            throw new InvalidOperationException("No experiment is expanded. Call 'I expand the first experiment' first.");

        // Record state before toggling
        var killSwitch = Page.Locator(
            ".kill-switch, input[type='checkbox'][name*='kill' i], button[data-action='kill']").First;

        _killSwitchStateBefore = await killSwitch.GetAttributeAsync("data-state")
                                 ?? await killSwitch.GetAttributeAsync("aria-checked")
                                 ?? (await killSwitch.IsCheckedAsync() ? "checked" : "unchecked");

        await _experimentsPage.ToggleKillSwitchAsync(_firstExperimentName);
    }

    [When(@"I click on a variant card")]
    public async Task WhenIClickOnAVariantCard()
    {
        var variantCard = Page.Locator(".variant-item, [data-variant], .variant-card").First;
        await variantCard.ClickAsync();
    }

    // -------------------------------------------------------------------------
    // Then
    // -------------------------------------------------------------------------

    [Then(@"I should see the experiments stats row")]
    public async Task ThenIShouldSeeTheExperimentsStatsRow()
    {
        await Page.WaitForSelectorAsync(
            ".stats-row, .experiment-stats, [data-stats]",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"the total experiments count should be greater than 0")]
    public async Task ThenTheTotalExperimentsCountShouldBeGreaterThan0()
    {
        var stats = await _experimentsPage.GetStatsAsync();
        if (stats.Total <= 0)
            throw new Exception($"Expected total experiments count to be greater than 0 but was {stats.Total}.");
    }

    [Then(@"I should see a list of experiments")]
    public async Task ThenIShouldSeeAListOfExperiments()
    {
        var count = await _experimentsPage.GetExperimentCountAsync();
        if (count == 0)
            throw new Exception("Expected a list of experiments but none were visible.");
    }

    [Then(@"each experiment should display its name and status")]
    public async Task ThenEachExperimentShouldDisplayItsNameAndStatus()
    {
        var items = Page.Locator(".experiment-item, .experiment-row, [data-experiment]");
        var count = await items.CountAsync();

        if (count == 0)
            throw new Exception("No experiment items found to verify.");

        for (var i = 0; i < count; i++)
        {
            var item = items.Nth(i);

            // Verify name — any non-empty heading / title element
            var nameEl = item.Locator(
                ".experiment-name, [data-experiment-name], h2, h3, .name, .title");
            var nameCount = await nameEl.CountAsync();
            if (nameCount == 0)
                throw new Exception($"Experiment item {i + 1} has no visible name element.");

            // Verify status badge is present
            var statusEl = item.Locator(
                ".status, .badge, [data-status], .experiment-status");
            var statusCount = await statusEl.CountAsync();
            if (statusCount == 0)
                throw new Exception($"Experiment item {i + 1} has no visible status element.");
        }
    }

    [Then(@"only experiments in the {string} category should be shown")]
    public async Task ThenOnlyExperimentsInTheCategoryShould(string category)
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var items = Page.Locator(".experiment-item, .experiment-row, [data-experiment]");
        var count = await items.CountAsync();

        if (count == 0)
            return; // No items is acceptable if the category has no experiments

        for (var i = 0; i < count; i++)
        {
            var text = await items.Nth(i).TextContentAsync() ?? string.Empty;
            if (!text.Contains(category, StringComparison.OrdinalIgnoreCase))
            {
                // Also allow data-category attribute
                var attr = await items.Nth(i).GetAttributeAsync("data-category");
                if (attr is null || !attr.Equals(category, StringComparison.OrdinalIgnoreCase))
                    throw new Exception(
                        $"Experiment item {i + 1} does not belong to category '{category}'.");
            }
        }
    }

    [Then(@"all experiments should be shown")]
    public async Task ThenAllExperimentsShouldBeShown()
    {
        await _experimentsPage.WaitForExperimentsLoadedAsync();
        var count = await _experimentsPage.GetExperimentCountAsync();
        if (count == 0)
            throw new Exception("Expected all experiments to be shown but no items are visible.");
    }

    [Then(@"the experiment list should be filtered to matching results")]
    public async Task ThenTheExperimentListShouldBeFilteredToMatchingResults()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        // Simply assert the list is still visible (filter applied without error)
        await Page.WaitForSelectorAsync(
            ".experiment-item, .experiment-row, [data-experiment], .no-results, .empty-state",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"I should see the experiment details panel")]
    public async Task ThenIShouldSeeTheExperimentDetailsPanel()
    {
        await Page.WaitForSelectorAsync(
            ".experiment-details, .details-panel, [data-experiment-details], .experiment-expanded",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"I should see variant cards")]
    public async Task ThenIShouldSeeVariantCards()
    {
        await Page.WaitForSelectorAsync(
            ".variant-item, [data-variant], .variant-card",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"the experiment details panel should be hidden")]
    public async Task ThenTheExperimentDetailsPanelShouldBeHidden()
    {
        var detailsPanel = Page.Locator(
            ".experiment-details, .details-panel, [data-experiment-details], .experiment-expanded");

        var count = await detailsPanel.CountAsync();
        if (count > 0)
        {
            await detailsPanel.First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden
            });
        }
    }

    [Then(@"the kill switch state should change")]
    public async Task ThenTheKillSwitchStateShouldChange()
    {
        if (_killSwitchStateBefore is null)
            throw new InvalidOperationException("Kill switch state was not captured before toggling.");

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var killSwitch = Page.Locator(
            ".kill-switch, input[type='checkbox'][name*='kill' i], button[data-action='kill']").First;

        var stateAfter = await killSwitch.GetAttributeAsync("data-state")
                         ?? await killSwitch.GetAttributeAsync("aria-checked")
                         ?? (await killSwitch.IsCheckedAsync() ? "checked" : "unchecked");

        if (stateAfter == _killSwitchStateBefore)
            throw new Exception(
                $"Expected kill switch state to change from '{_killSwitchStateBefore}' but it remained the same.");
    }

    [Then(@"the variant should be marked as active")]
    public async Task ThenTheVariantShouldBeMarkedAsActive()
    {
        await Page.WaitForSelectorAsync(
            ".variant-item.active, [data-variant][data-active='true'], .variant-card.active, " +
            ".variant-item [aria-pressed='true'], .variant-card .active-indicator",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"I should see skeleton loading placeholders")]
    public async Task ThenIShouldSeeSkeletonLoadingPlaceholders()
    {
        await Page.WaitForSelectorAsync(
            ".skeleton, .skeleton-loader, [data-skeleton], .loading-placeholder, .shimmer",
            new PageWaitForSelectorOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 5000
            });
    }
}
