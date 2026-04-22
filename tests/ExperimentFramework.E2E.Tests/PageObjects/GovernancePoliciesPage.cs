using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the governance policies page at <c>/dashboard/governance/policies</c>.
/// </summary>
public class GovernancePoliciesPage
{
    private readonly IPage _page;

    private ILocator PageContainer         => _page.Locator(".governance-policies, [data-page='policies'], main");
    private ILocator ExperimentSelect      => _page.Locator("select[name*='experiment' i], [data-select='experiment'], .experiment-select");
    private ILocator ComplianceSummary     => _page.Locator(".compliance-summary, [data-compliance-summary], .summary-section");
    private ILocator PolicyCards           => _page.Locator(".policy-card, [data-policy-card], .policy-item");
    private ILocator NotConfiguredMessage  => _page.Locator(".not-configured, [data-not-configured], .empty-state");

    public GovernancePoliciesPage(IPage page)
    {
        _page = page;
    }

    /// <summary>Verifies the policies page container is visible.</summary>
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

    /// <summary>Returns the text content of the compliance summary section.</summary>
    public async Task<string> GetComplianceSummaryAsync() =>
        (await ComplianceSummary.TextContentAsync() ?? string.Empty).Trim();

    /// <summary>Returns the text content of each policy card.</summary>
    public async Task<IReadOnlyList<string>> GetPolicyCardsAsync()
    {
        var count = await PolicyCards.CountAsync();
        var cards = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var text = await PolicyCards.Nth(i).TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
                cards.Add(text.Trim());
        }

        return cards;
    }

    /// <summary>Returns true when the "not configured" / empty-state message is displayed.</summary>
    public async Task<bool> IsNotConfiguredAsync() =>
        await NotConfiguredMessage.IsVisibleAsync();

    // -----------------------------------------------------------------------
    // Assertion helpers (called directly from step definitions)
    // -----------------------------------------------------------------------

    /// <summary>Waits until the page container is visible.</summary>
    public async Task WaitForPageLoadAsync() =>
        await PageContainer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

    /// <summary>
    /// Returns true when the governance persistence backplane is configured
    /// (i.e. the not-configured message is NOT shown).
    /// </summary>
    public async Task<bool> IsConfiguredAsync() =>
        !await IsNotConfiguredAsync();

    /// <summary>Asserts the compliance summary section is visible.</summary>
    public async Task AssertComplianceSummaryVisibleAsync() =>
        await ComplianceSummary.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

    /// <summary>Asserts the not-configured message is visible.</summary>
    public async Task AssertNotConfiguredMessageVisibleAsync() =>
        await NotConfiguredMessage.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

    /// <summary>
    /// Asserts that each policy card has a visible compliant or non-compliant status indicator.
    /// </summary>
    public async Task AssertPolicyCardsHaveStatusAsync()
    {
        var count = await PolicyCards.CountAsync();
        for (var i = 0; i < count; i++)
        {
            var statusEl = PolicyCards.Nth(i).Locator(".status, .badge, [data-status], .policy-status");
            await statusEl.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        }
    }
}
