using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the experiments list page at <c>/dashboard/experiments</c>.
/// </summary>
public class ExperimentsPage
{
    private readonly IPage _page;

    private ILocator PageContainer      => _page.Locator(".experiments-container, [data-page='experiments'], main");
    private ILocator ExperimentItems    => _page.Locator(".experiment-item, .experiment-row, [data-experiment]");
    private ILocator StatsRow           => _page.Locator(".stats-row, .experiment-stats, [data-stats]");
    private ILocator SearchInput        => _page.Locator("input[type='search'], input[placeholder*='search' i], input[name*='search' i]");
    private ILocator CategoryButtons    => _page.Locator(".category-filter button, .category-btn, [data-category]");
    private ILocator LoadingIndicator   => _page.Locator(".loading, .spinner, [data-loading='true']");

    public ExperimentsPage(IPage page)
    {
        _page = page;
    }

    /// <summary>Verifies the experiments page container is visible.</summary>
    public async Task<bool> IsLoadedAsync()
    {
        try
        {
            await PageContainer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    /// <summary>Waits until the experiments list has finished loading (loading indicator gone).</summary>
    public async Task WaitForExperimentsLoadedAsync()
    {
        // Wait for loading indicators to disappear
        var count = await LoadingIndicator.CountAsync();
        if (count > 0)
        {
            await LoadingIndicator.First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden
            });
        }

        await PageContainer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }

    /// <summary>Returns the number of experiment items currently displayed.</summary>
    public Task<int> GetExperimentCountAsync() =>
        ExperimentItems.CountAsync();

    /// <summary>Fills the search input and triggers search.</summary>
    public async Task SearchAsync(string query)
    {
        await SearchInput.FillAsync(query);
        await SearchInput.PressAsync("Enter");
    }

    /// <summary>Clicks the category filter button matching <paramref name="category"/>.</summary>
    public async Task FilterByCategoryAsync(string category)
    {
        var button = CategoryButtons.Filter(new LocatorFilterOptions { HasText = category });
        await button.First.ClickAsync();
    }

    /// <summary>Expands (opens) the experiment row with the given <paramref name="name"/>.</summary>
    public async Task ExpandExperimentAsync(string name)
    {
        var item = GetExperimentLocator(name);
        var toggle = item.Locator(".expand-toggle, .collapse-toggle, [aria-expanded='false'], button.toggle");
        await toggle.First.ClickAsync();
    }

    /// <summary>Collapses (closes) the experiment row with the given <paramref name="name"/>.</summary>
    public async Task CollapseExperimentAsync(string name)
    {
        var item = GetExperimentLocator(name);
        var toggle = item.Locator("[aria-expanded='true'], button.toggle");
        await toggle.First.ClickAsync();
    }

    /// <summary>Toggles the kill switch for the named experiment.</summary>
    public async Task ToggleKillSwitchAsync(string experimentName)
    {
        var item = GetExperimentLocator(experimentName);
        var killSwitch = item.Locator(".kill-switch, input[type='checkbox'][name*='kill' i], button[data-action='kill']");
        await killSwitch.First.ClickAsync();
    }

    /// <summary>Activates a specific variant within the named experiment.</summary>
    public async Task ActivateVariantAsync(string experimentName, string variantName)
    {
        var item = GetExperimentLocator(experimentName);
        var variant = item
            .Locator(".variant-item, [data-variant]")
            .Filter(new LocatorFilterOptions { HasText = variantName });

        var activateBtn = variant.Locator("button[data-action='activate'], .activate-btn, button:has-text('Activate')");
        await activateBtn.First.ClickAsync();
    }

    /// <summary>
    /// Reads the summary statistics bar and returns a named tuple with
    /// Total, Active, Killed, and Variants counts.
    /// </summary>
    public async Task<(int Total, int Active, int Killed, int Variants)> GetStatsAsync()
    {
        var text = await StatsRow.TextContentAsync() ?? string.Empty;
        return ParseStats(text);
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private ILocator GetExperimentLocator(string name) =>
        ExperimentItems.Filter(new LocatorFilterOptions { HasText = name });

    private static (int Total, int Active, int Killed, int Variants) ParseStats(string text)
    {
        static int Extract(string src, string label)
        {
            var idx = src.IndexOf(label, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return 0;
            // Walk backwards to find the number preceding the label
            var slice = src[..idx].TrimEnd();
            var numStart = slice.Length - 1;
            while (numStart >= 0 && char.IsDigit(slice[numStart]))
                numStart--;
            return int.TryParse(slice[(numStart + 1)..], out var n) ? n : 0;
        }

        return (
            Total:    Extract(text, "Total"),
            Active:   Extract(text, "Active"),
            Killed:   Extract(text, "Killed"),
            Variants: Extract(text, "Variant")
        );
    }
}
