using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the governance versions page at <c>/dashboard/governance/versions</c>.
/// </summary>
public class GovernanceVersionsPage : IGovernanceSelectable
{
    private readonly IPage _page;

    private ILocator PageContainer     => _page.Locator(".governance-versions, [data-page='versions'], main");
    private ILocator ExperimentSelect  => _page.Locator("select[name*='experiment' i], [data-select='experiment'], .experiment-select");
    private ILocator VersionList       => _page.Locator(".version-item, [data-version-item], tr.version-row");
    private ILocator VersionViewer     => _page.Locator(".version-viewer, [data-version-viewer], .version-detail");
    private ILocator CloseViewerButton => _page.Locator("button:has-text('Close'), button[data-action='close-viewer'], .close-btn");
    private ILocator ConfirmRollbackBtn => _page.Locator("button:has-text('Confirm'), button[data-action='confirm-rollback'], .confirm-rollback");

    public GovernanceVersionsPage(IPage page)
    {
        _page = page;
    }

    /// <summary>Verifies the versions page container is visible.</summary>
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

    /// <summary>Returns the text content of each version entry in the list.</summary>
    public async Task<IReadOnlyList<string>> GetVersionsAsync()
    {
        var count = await VersionList.CountAsync();
        var versions = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var text = await VersionList.Nth(i).TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
                versions.Add(text.Trim());
        }

        return versions;
    }

    /// <summary>Clicks the View button for the specified version number.</summary>
    public async Task ViewVersionAsync(int versionNumber)
    {
        var row = VersionList.Filter(new LocatorFilterOptions { HasText = versionNumber.ToString() });
        var viewBtn = row.Locator("button:has-text('View'), button[data-action='view']");
        await viewBtn.First.ClickAsync();
        await VersionViewer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }

    /// <summary>Clicks the Rollback button for the specified version number.</summary>
    public async Task RollbackToVersionAsync(int versionNumber)
    {
        var row = VersionList.Filter(new LocatorFilterOptions { HasText = versionNumber.ToString() });
        var rollbackBtn = row.Locator("button:has-text('Rollback'), button[data-action='rollback']");
        await rollbackBtn.First.ClickAsync();
    }

    /// <summary>Confirms a pending rollback action.</summary>
    public Task ConfirmRollbackAsync() =>
        ConfirmRollbackBtn.ClickAsync();

    /// <summary>Closes the version viewer panel.</summary>
    public async Task CloseViewerAsync()
    {
        await CloseViewerButton.ClickAsync();
        await VersionViewer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
    }

    // -----------------------------------------------------------------------
    // Assertion / convenience helpers (called directly from step definitions)
    // -----------------------------------------------------------------------

    /// <summary>Waits until the page container is visible.</summary>
    public async Task WaitForPageLoadAsync() =>
        await PageContainer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

    /// <summary>Selects the first available experiment in the dropdown (IGovernanceSelectable).</summary>
    public async Task SelectFirstExperimentAsync()
    {
        // In InteractiveServer mode the dropdown is absent during the Blazor circuit
        // reconnect phase (_loading=true).  Wait for the first real option (index 1 —
        // index 0 is the placeholder) before selecting so we don't accidentally pick
        // an empty value and leave the experiment unselected.
        var options = ExperimentSelect.Locator("option");
        await options.Nth(1).WaitForAsync(new LocatorWaitForOptions
        {
            State   = WaitForSelectorState.Attached,
            Timeout = 15_000,
        });

        var count = await options.CountAsync();
        var idx = count > 1 ? 1 : 0;
        var value = await options.Nth(idx).GetAttributeAsync("value");
        if (value is not null)
            await ExperimentSelect.SelectOptionAsync(new SelectOptionValue { Value = value });
    }

    /// <summary>Clicks the View button on the first version in the list.</summary>
    public async Task ClickViewFirstVersionAsync()
    {
        // Wait for the version list to populate after the experiment is selected.
        await VersionList.First.WaitForAsync(new LocatorWaitForOptions
        {
            State   = WaitForSelectorState.Visible,
            Timeout = 15_000,
        });
        var viewBtn = VersionList.First.Locator("button:has-text('View'), button[data-action='view']");
        await viewBtn.ClickAsync();
        await VersionViewer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }

    /// <summary>Closes the version viewer (alias for CloseViewerAsync for step compatibility).</summary>
    public Task CloseVersionViewerAsync() => CloseViewerAsync();

    /// <summary>Asserts the version history list has at least one entry.</summary>
    public async Task AssertVersionHistoryVisibleAsync() =>
        await VersionList.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

    /// <summary>Asserts the version detail modal / panel is visible and contains JSON-like content.</summary>
    public async Task AssertVersionDetailModalVisibleAsync()
    {
        await VersionViewer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        var content = await VersionViewer.TextContentAsync() ?? string.Empty;
        if (!content.Contains("{") && !content.Contains("version", StringComparison.OrdinalIgnoreCase))
            throw new Exception("Version detail modal is visible but does not appear to contain JSON content.");
    }

    /// <summary>Asserts the version viewer panel is hidden.</summary>
    public async Task AssertVersionViewerHiddenAsync()
    {
        var count = await VersionViewer.CountAsync();
        if (count > 0)
            await VersionViewer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
    }
}
