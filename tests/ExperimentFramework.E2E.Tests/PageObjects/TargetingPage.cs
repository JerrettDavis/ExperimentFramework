using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the targeting page at <c>/dashboard/targeting</c>.
/// This is a read-only view — all controls are disabled.
/// </summary>
public class TargetingPage
{
    private readonly IPage _page;

    private ILocator PageContainer     => _page.Locator(".targeting-container, [data-page='targeting'], main");
    private ILocator TargetingRules    => _page.Locator(".targeting-rule, [data-targeting-rule], .rule-item");
    private ILocator RefreshButton     => _page.Locator("button:has-text('Refresh'), button[data-action='refresh'], .refresh-btn");

    public TargetingPage(IPage page)
    {
        _page = page;
    }

    /// <summary>Verifies the targeting page container is visible.</summary>
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

    /// <summary>Returns the text content of each targeting rule displayed (read-only).</summary>
    public async Task<IReadOnlyList<string>> GetTargetingRulesAsync()
    {
        var count = await TargetingRules.CountAsync();
        var rules = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var text = await TargetingRules.Nth(i).TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
                rules.Add(text.Trim());
        }

        return rules;
    }

    /// <summary>Clicks the Refresh button and waits for the network to settle.</summary>
    public async Task RefreshAsync()
    {
        await RefreshButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
