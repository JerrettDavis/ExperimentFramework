using ExperimentFramework.E2E.Tests.Drivers;
using ExperimentFramework.E2E.Tests.PageObjects;
using Microsoft.Playwright;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.StepDefinitions.Experiments;

/// <summary>
/// Step definitions specific to the Tutorial 1 "Your First Experiment" docs scenario.
/// These steps are intentionally named to match the tutorial narrative.
/// </summary>
[Binding]
public class DocsTutorial1StepDefinitions
{
    private readonly BrowserDriver _browser;
    private readonly DashboardDriver _dashboard;
    private readonly ExperimentsPage _experimentsPage;

    public DocsTutorial1StepDefinitions(BrowserDriver browser, DashboardDriver dashboard)
    {
        _browser         = browser;
        _dashboard       = dashboard;
        _experimentsPage = new ExperimentsPage(browser.Page);
    }

    private IPage Page => _browser.Page;

    /// <summary>
    /// Waits for the experiments list to finish its initial data-load.
    /// Blazor server-side renders a skeleton while the API call completes;
    /// this step ensures real data is visible before the screenshot fires.
    /// </summary>
    [When(@"I wait for the experiments list to load")]
    public async Task WhenIWaitForTheExperimentsListToLoad()
    {
        // Wait for at least one named experiment to appear (skeleton rows have no text).
        // The Experiments.razor page uses .experiments-console as the container and
        // .exp-name for individual experiment name spans.
        await Page.WaitForSelectorAsync(
            ".experiments-console",
            new PageWaitForSelectorOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 20_000
            });

        // Wait for real data rows: non-skeleton experiment rows
        await Page.WaitForSelectorAsync(
            ".experiment-row:not(.skeleton)",
            new PageWaitForSelectorOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 20_000
            });

        // Small settle delay so Blazor fully paints the stats row numbers.
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>Expands the experiment row identified by <paramref name="name"/>.</summary>
    [When(@"I expand the experiment named {string}")]
    public async Task WhenIExpandTheExperimentNamed(string name)
    {
        // Click the row — the Experiments.razor page uses the row header as the toggle.
        var row = Page.Locator($".experiment-row")
            .Filter(new LocatorFilterOptions { HasText = name });

        // The exp-main div is the clickable expand area.
        var expMain = row.Locator(".exp-main");
        await expMain.First.ClickAsync();

        // Wait for the detail panel to appear.
        await Page.WaitForSelectorAsync(
            ".exp-details",
            new PageWaitForSelectorOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 5_000
            });
    }

    /// <summary>Collapses the experiment row identified by <paramref name="name"/>.</summary>
    [When(@"I collapse the experiment named {string}")]
    public async Task WhenICollapseTheExperimentNamed(string name)
    {
        var row = Page.Locator(".experiment-row.expanded")
            .Filter(new LocatorFilterOptions { HasText = name });

        var expMain = row.Locator(".exp-main");
        await expMain.First.ClickAsync();

        // Wait for the detail panel to disappear.
        var detail = Page.Locator(".exp-details");
        var count  = await detail.CountAsync();
        if (count > 0)
        {
            await detail.First.WaitForAsync(new LocatorWaitForOptions
            {
                State   = WaitForSelectorState.Hidden,
                Timeout = 5_000
            });
        }
    }

    /// <summary>
    /// Toggles the kill-switch for the named experiment.
    /// The UI applies an optimistic update immediately, which is enough to capture
    /// a "toggled" screenshot before any API revert fires.
    /// The Experiments.razor uses a custom CSS toggle: a hidden checkbox wrapped in a
    /// label with a .toggle-track span. We click the visible .toggle-track element.
    /// </summary>
    [When(@"I toggle the killswitch for {string}")]
    public async Task WhenIToggleTheKillswitchFor(string name)
    {
        // The experiment must already be expanded (exp-details visible).
        var row = Page.Locator(".experiment-row.expanded")
            .Filter(new LocatorFilterOptions { HasText = name });

        // Use the label (the entire custom toggle component) which is visible and clickable.
        // .toggle is the <label> wrapper around the hidden checkbox + .toggle-track.
        var toggleLabel = row.Locator("label.toggle");
        await toggleLabel.First.ClickAsync();

        // Give the optimistic UI update a moment to render.
        await Page.WaitForTimeoutAsync(300);
    }
}
