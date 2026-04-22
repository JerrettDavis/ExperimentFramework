using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the governance versions page at <c>/dashboard/governance/versions</c>.
/// </summary>
public class GovernanceVersionsPage
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
}
