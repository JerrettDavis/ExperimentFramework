using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the rollout management page at <c>/dashboard/rollout</c>.
/// </summary>
public class RolloutPage
{
    private readonly IPage _page;

    private ILocator PageContainer       => _page.Locator(".rollout-container, [data-page='rollout'], main");
    private ILocator ExperimentSelect    => _page.Locator("select[name*='experiment' i], [data-select='experiment'], .experiment-select");
    private ILocator VariantSelect       => _page.Locator("select[name*='variant' i], [data-select='variant'], .variant-select");
    private ILocator StageNameInput      => _page.Locator("input[name*='stage' i][name*='name' i], input[placeholder*='stage name' i]");
    private ILocator StagePercentInput   => _page.Locator("input[type='number'][name*='percent' i], input[placeholder*='percent' i]");
    private ILocator StageDurationInput  => _page.Locator("input[type='number'][name*='duration' i], input[placeholder*='hours' i]");
    private ILocator AddStageButton      => _page.Locator("button:has-text('Add Stage'), button[data-action='add-stage']");
    private ILocator StartButton         => _page.Locator("button:has-text('Start'), button[data-action='start-rollout']");
    private ILocator PauseButton         => _page.Locator("button:has-text('Pause'), button[data-action='pause']");
    private ILocator AdvanceButton       => _page.Locator("button:has-text('Advance'), button[data-action='advance']");
    private ILocator RollbackButton      => _page.Locator("button:has-text('Rollback'), button[data-action='rollback']");
    private ILocator ResumeButton        => _page.Locator("button:has-text('Resume'), button[data-action='resume']");
    private ILocator RestartButton       => _page.Locator("button:has-text('Restart'), button[data-action='restart']");
    private ILocator DeleteButton        => _page.Locator("button:has-text('Delete'), button[data-action='delete-rollout']");
    private ILocator ProgressBar         => _page.Locator(".rollout-progress, progress, [data-progress], [role='progressbar']");
    private ILocator StatusBadge         => _page.Locator(".status-badge, [data-status], .rollout-status");
    private ILocator StageList           => _page.Locator(".stage-item, [data-stage], .rollout-stage");

    public RolloutPage(IPage page)
    {
        _page = page;
    }

    /// <summary>Verifies the rollout page container is visible.</summary>
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

    /// <summary>Selects an experiment from the experiment dropdown.</summary>
    public Task SelectExperimentAsync(string name) =>
        ExperimentSelect.SelectOptionAsync(new SelectOptionValue { Label = name });

    /// <summary>Selects a variant from the variant dropdown.</summary>
    public Task SelectVariantAsync(string variant) =>
        VariantSelect.SelectOptionAsync(new SelectOptionValue { Label = variant });

    /// <summary>Fills the new-stage form and adds it to the rollout plan.</summary>
    public async Task AddStageAsync(string name, int percentage, int durationHours)
    {
        await StageNameInput.FillAsync(name);
        await StagePercentInput.FillAsync(percentage.ToString());
        await StageDurationInput.FillAsync(durationHours.ToString());
        await AddStageButton.ClickAsync();
    }

    /// <summary>Removes the stage at zero-based <paramref name="index"/>.</summary>
    public async Task RemoveStageAsync(int index)
    {
        var removeBtn = StageList.Nth(index)
            .Locator("button:has-text('Remove'), button[data-action='remove-stage']");
        await removeBtn.ClickAsync();
    }

    /// <summary>Starts the rollout.</summary>
    public Task StartRolloutAsync() =>
        StartButton.ClickAsync();

    /// <summary>Pauses the active rollout.</summary>
    public Task PauseRolloutAsync() =>
        PauseButton.ClickAsync();

    /// <summary>Advances to the next stage.</summary>
    public Task AdvanceStageAsync() =>
        AdvanceButton.ClickAsync();

    /// <summary>Initiates a rollback.</summary>
    public Task RollbackAsync() =>
        RollbackButton.ClickAsync();

    /// <summary>Resumes a paused rollout.</summary>
    public Task ResumeAsync() =>
        ResumeButton.ClickAsync();

    /// <summary>Restarts the rollout from the beginning.</summary>
    public Task RestartAsync() =>
        RestartButton.ClickAsync();

    /// <summary>Deletes the current rollout plan.</summary>
    public Task DeleteRolloutAsync() =>
        DeleteButton.ClickAsync();

    /// <summary>
    /// Returns the current progress percentage from the progress bar,
    /// or 0 if no progress bar is found.
    /// </summary>
    public async Task<int> GetProgressPercentageAsync()
    {
        var count = await ProgressBar.CountAsync();
        if (count == 0) return 0;

        // Try value attribute (HTML5 <progress>) or aria-valuenow
        var value = await ProgressBar.First.GetAttributeAsync("value")
                    ?? await ProgressBar.First.GetAttributeAsync("aria-valuenow");

        return int.TryParse(value, out var pct) ? pct : 0;
    }

    /// <summary>Returns the text of the current status badge.</summary>
    public async Task<string> GetCurrentStatusAsync() =>
        (await StatusBadge.First.TextContentAsync() ?? string.Empty).Trim();
}
