using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the governance lifecycle page at <c>/dashboard/governance/lifecycle</c>.
/// </summary>
public class GovernanceLifecyclePage : IGovernanceSelectable
{
    private readonly IPage _page;

    private ILocator PageContainer         => _page.Locator(".governance-lifecycle, [data-page='lifecycle'], main");
    private ILocator ExperimentSelect      => _page.Locator("select[name*='experiment' i], [data-select='experiment'], .experiment-select");
    private ILocator CurrentStateDisplay   => _page.Locator(".current-state, [data-current-state], .lifecycle-state");
    private ILocator TransitionButtons     => _page.Locator(".transition-btn, button[data-transition], .available-transition");
    private ILocator TransitionHistory     => _page.Locator(".transition-history li, .history-entry, [data-history-entry]");
    private ILocator ActorInput            => _page.Locator("input[name*='actor' i], input[placeholder*='actor' i]");
    private ILocator ReasonInput           => _page.Locator("textarea[name*='reason' i], input[name*='reason' i], textarea[placeholder*='reason' i]");
    private ILocator NotConfiguredMessage  => _page.Locator(".not-configured, [data-not-configured], .empty-state");

    public GovernanceLifecyclePage(IPage page)
    {
        _page = page;
    }

    /// <summary>Verifies the lifecycle page container is visible.</summary>
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

    /// <summary>Returns the text of the current lifecycle state.</summary>
    public async Task<string> GetCurrentStateAsync() =>
        (await CurrentStateDisplay.TextContentAsync() ?? string.Empty).Trim();

    /// <summary>Returns the labels of all available transition buttons.</summary>
    public async Task<IReadOnlyList<string>> GetAvailableTransitionsAsync()
    {
        var count = await TransitionButtons.CountAsync();
        var transitions = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var text = await TransitionButtons.Nth(i).TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
                transitions.Add(text.Trim());
        }

        return transitions;
    }

    /// <summary>
    /// Fills the actor/reason fields and clicks the button to transition to <paramref name="state"/>.
    /// </summary>
    public async Task TransitionToAsync(string state, string actor, string reason)
    {
        var actorCount = await ActorInput.CountAsync();
        if (actorCount > 0)
            await ActorInput.FillAsync(actor);

        var reasonCount = await ReasonInput.CountAsync();
        if (reasonCount > 0)
            await ReasonInput.FillAsync(reason);

        var btn = TransitionButtons.Filter(new LocatorFilterOptions { HasText = state });
        await btn.First.ClickAsync();
    }

    /// <summary>Returns the text content of each entry in the transition history list.</summary>
    public async Task<IReadOnlyList<string>> GetTransitionHistoryAsync()
    {
        var count = await TransitionHistory.CountAsync();
        var entries = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var text = await TransitionHistory.Nth(i).TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
                entries.Add(text.Trim());
        }

        return entries;
    }

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

    /// <summary>Selects the first experiment option in the dropdown.</summary>
    public async Task SelectFirstExperimentAsync()
    {
        var options = ExperimentSelect.Locator("option");
        var count = await options.CountAsync();
        // Skip the placeholder (index 0) if there are more options
        var idx = count > 1 ? 1 : 0;
        var value = await options.Nth(idx).GetAttributeAsync("value");
        if (value is not null)
            await ExperimentSelect.SelectOptionAsync(new SelectOptionValue { Value = value });
    }

    /// <summary>Clicks the first available transition button.</summary>
    public async Task ClickFirstTransitionAsync() =>
        await TransitionButtons.First.ClickAsync();

    /// <summary>Fills the actor and reason fields used in a transition confirmation form.</summary>
    public async Task FillTransitionFormAsync(string actor, string reason)
    {
        var actorCount = await ActorInput.CountAsync();
        if (actorCount > 0) await ActorInput.FillAsync(actor);

        var reasonCount = await ReasonInput.CountAsync();
        if (reasonCount > 0) await ReasonInput.FillAsync(reason);
    }

    /// <summary>Clicks the confirm button on a transition dialog.</summary>
    public async Task ConfirmTransitionAsync()
    {
        var confirmBtn = _page.Locator("button:has-text('Confirm'), button[data-action='confirm']");
        await confirmBtn.First.ClickAsync();
    }

    /// <summary>Asserts the not-configured message is visible.</summary>
    public async Task AssertNotConfiguredMessageVisibleAsync() =>
        await NotConfiguredMessage.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

    /// <summary>Asserts the current-state element is visible.</summary>
    public async Task AssertCurrentStateVisibleAsync() =>
        await CurrentStateDisplay.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

    /// <summary>Asserts that at least one transition button is visible.</summary>
    public async Task AssertTransitionsVisibleAsync() =>
        await TransitionButtons.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

    /// <summary>Asserts the transition history section is visible.</summary>
    public async Task AssertTransitionHistoryVisibleAsync() =>
        await TransitionHistory.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

    /// <summary>Asserts the current state has changed from <paramref name="previousState"/>.</summary>
    public async Task AssertStateUpdatedAsync(string? previousState)
    {
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var newState = await GetCurrentStateAsync();
        if (newState == previousState)
            throw new Exception(
                $"Expected the governance state to change from '{previousState}' but it is still '{newState}'.");
    }
}
