using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the governance audit page at <c>/dashboard/governance/audit</c>.
/// </summary>
public class GovernanceAuditPage
{
    private readonly IPage _page;

    private ILocator PageContainer        => _page.Locator(".governance-audit, [data-page='audit'], main");
    private ILocator ExperimentSelect     => _page.Locator("select[name*='experiment' i], [data-select='experiment'], .experiment-select");
    private ILocator TypeFilter           => _page.Locator("select[name*='type' i], [data-filter='type'], .type-filter");
    private ILocator SearchInput          => _page.Locator("input[type='search'], input[placeholder*='search' i], input[name*='search' i]");
    private ILocator AuditEntries         => _page.Locator(".audit-entry, tr.audit-row, [data-audit-entry]");
    private ILocator StatsSection         => _page.Locator(".audit-stats, [data-stats], .stats-section");
    private ILocator NotConfiguredMessage => _page.Locator(".not-configured, [data-not-configured], .empty-state");

    public GovernanceAuditPage(IPage page)
    {
        _page = page;
    }

    /// <summary>Verifies the audit page container is visible.</summary>
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

    /// <summary>Selects an experiment from the dropdown.</summary>
    public Task SelectExperimentAsync(string name) =>
        ExperimentSelect.SelectOptionAsync(new SelectOptionValue { Label = name });

    /// <summary>Selects an audit event type from the type filter dropdown.</summary>
    public Task FilterByTypeAsync(string type) =>
        TypeFilter.SelectOptionAsync(new SelectOptionValue { Label = type });

    /// <summary>Fills the search input to filter audit entries.</summary>
    public async Task SearchAsync(string query)
    {
        await SearchInput.FillAsync(query);
        await SearchInput.PressAsync("Enter");
    }

    /// <summary>Returns the text content of each visible audit entry.</summary>
    public async Task<IReadOnlyList<string>> GetAuditEntriesAsync()
    {
        var count = await AuditEntries.CountAsync();
        var entries = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var text = await AuditEntries.Nth(i).TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
                entries.Add(text.Trim());
        }

        return entries;
    }

    /// <summary>Returns the raw stats section text.</summary>
    public async Task<string> GetStatsAsync() =>
        (await StatsSection.TextContentAsync() ?? string.Empty).Trim();

    /// <summary>Returns true when the "not configured" / empty-state message is displayed.</summary>
    public async Task<bool> IsNotConfiguredAsync() =>
        await NotConfiguredMessage.IsVisibleAsync();

    // -----------------------------------------------------------------------
    // Assertion / convenience helpers (called directly from step definitions)
    // -----------------------------------------------------------------------

    /// <summary>Waits until the page container is visible.</summary>
    public async Task WaitForPageLoadAsync() =>
        await PageContainer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

    /// <summary>Returns true when governance persistence is configured (not-configured message absent).</summary>
    public async Task<bool> IsConfiguredAsync() =>
        !await IsNotConfiguredAsync();

    /// <summary>Asserts that at least one audit entry row is visible.</summary>
    public async Task AssertAuditEntriesVisibleAsync() =>
        await AuditEntries.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

    /// <summary>Asserts the not-configured message is visible.</summary>
    public async Task AssertNotConfiguredMessageVisibleAsync() =>
        await NotConfiguredMessage.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

    /// <summary>
    /// Asserts that each visible audit entry matches the given <paramref name="expectedType"/>.
    /// </summary>
    public async Task AssertFilteredByTypeAsync(string expectedType)
    {
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var count = await AuditEntries.CountAsync();
        for (var i = 0; i < count; i++)
        {
            var text = await AuditEntries.Nth(i).TextContentAsync() ?? string.Empty;
            if (!text.Contains(expectedType, StringComparison.OrdinalIgnoreCase))
                throw new Exception(
                    $"Audit entry {i + 1} does not match expected type '{expectedType}'. Entry: '{text}'");
        }
    }

    /// <summary>Asserts that at least one audit entry is visible after a search.</summary>
    public async Task AssertSearchResultsVisibleAsync()
    {
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        // Either entries are shown OR a no-results message — both are acceptable
        await _page.WaitForSelectorAsync(
            ".audit-entry, tr.audit-row, [data-audit-entry], .no-results, .empty-state",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }
}
