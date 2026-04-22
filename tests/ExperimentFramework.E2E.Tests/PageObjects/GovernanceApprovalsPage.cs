using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the governance approvals page at <c>/dashboard/governance/approvals</c>.
/// This is a static / informational page.
/// </summary>
public class GovernanceApprovalsPage
{
    private readonly IPage _page;

    private ILocator PageContainer  => _page.Locator(".governance-approvals, [data-page='approvals'], main");
    private ILocator WorkflowSteps  => _page.Locator(".workflow-step, [data-workflow-step], .step-item");
    private ILocator FeatureCards   => _page.Locator(".feature-card, [data-feature-card]");

    public GovernanceApprovalsPage(IPage page)
    {
        _page = page;
    }

    /// <summary>Verifies the approvals page container is visible.</summary>
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

    /// <summary>Returns the text content of each workflow step displayed on the page.</summary>
    public async Task<IReadOnlyList<string>> GetWorkflowStepsAsync()
    {
        var count = await WorkflowSteps.CountAsync();
        var steps = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var text = await WorkflowSteps.Nth(i).TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
                steps.Add(text.Trim());
        }

        return steps;
    }

    /// <summary>Returns the text content of each feature card on the page.</summary>
    public async Task<IReadOnlyList<string>> GetFeatureCardsAsync()
    {
        var count = await FeatureCards.CountAsync();
        var cards = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var text = await FeatureCards.Nth(i).TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
                cards.Add(text.Trim());
        }

        return cards;
    }

    // -----------------------------------------------------------------------
    // Assertion helpers (called directly from step definitions)
    // -----------------------------------------------------------------------

    /// <summary>Asserts at least one workflow step element is visible.</summary>
    public async Task AssertWorkflowStepsVisibleAsync() =>
        await WorkflowSteps.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

    /// <summary>Asserts at least one feature card element is visible.</summary>
    public async Task AssertFeatureCardsVisibleAsync() =>
        await FeatureCards.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

    /// <summary>Asserts that navigation links to other governance pages are visible.</summary>
    public async Task AssertGovernanceNavigationLinksVisibleAsync()
    {
        // Governance pages are linked either in nav or in the page body
        var governanceLinks = _page.Locator(
            "a[href*='governance'], [data-governance-link], .governance-nav-link");
        await governanceLinks.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }
}
