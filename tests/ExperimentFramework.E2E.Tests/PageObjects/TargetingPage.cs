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

    // -----------------------------------------------------------------------
    // Assertion / convenience helpers (called directly from step definitions)
    // -----------------------------------------------------------------------

    /// <summary>Waits until the page container is visible.</summary>
    public async Task WaitForPageLoadAsync() =>
        await PageContainer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

    /// <summary>Alias for RefreshAsync — clicks the refresh button.</summary>
    public Task ClickRefreshAsync() => RefreshAsync();

    /// <summary>Asserts the targeting rules display area is visible.</summary>
    public async Task AssertRulesDisplayVisibleAsync() =>
        await PageContainer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

    /// <summary>Asserts that all toggle switches on the page are disabled (read-only view).</summary>
    public async Task AssertAllTogglesDisabledAsync()
    {
        var toggles = _page.Locator("input[type='checkbox'], input[type='range'], .toggle-switch");
        var count = await toggles.CountAsync();
        for (var i = 0; i < count; i++)
        {
            var isDisabled = await toggles.Nth(i).IsDisabledAsync();
            if (!isDisabled)
                throw new Exception($"Toggle at index {i} is not disabled — targeting page should be read-only.");
        }
    }

    /// <summary>Asserts each targeting rule has at least one condition tag element.</summary>
    public async Task AssertRulesHaveConditionTagsAsync()
    {
        var count = await TargetingRules.CountAsync();
        if (count == 0) return; // No rules is valid

        for (var i = 0; i < count; i++)
        {
            var tags = TargetingRules.Nth(i)
                .Locator(".condition-tag, .tag, [data-condition], .rule-condition");
            var tagCount = await tags.CountAsync();
            if (tagCount == 0)
                throw new Exception($"Targeting rule at index {i} has no condition tags.");
        }
    }

    /// <summary>Asserts each targeting rule has a target variant indicator.</summary>
    public async Task AssertRulesHaveTargetVariantAsync()
    {
        var count = await TargetingRules.CountAsync();
        if (count == 0) return;

        for (var i = 0; i < count; i++)
        {
            var variantEl = TargetingRules.Nth(i)
                .Locator(".target-variant, [data-target-variant], .variant-badge, .rule-variant");
            var variantCount = await variantEl.CountAsync();
            if (variantCount == 0)
                throw new Exception($"Targeting rule at index {i} has no target variant element.");
        }
    }

    /// <summary>Asserts the targeting rules are visible after a reload.</summary>
    public async Task AssertRulesReloadedAsync()
    {
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await PageContainer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }
}
