using ExperimentFramework.E2E.Tests.Drivers;
using ExperimentFramework.E2E.Tests.PageObjects;
using Microsoft.Playwright;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.StepDefinitions.Governance;

[Binding]
public class GovernanceApprovalsStepDefinitions
{
    private readonly BrowserDriver _browser;
    private readonly DashboardDriver _dashboard;
    private readonly GovernanceApprovalsPage _page;

    public GovernanceApprovalsStepDefinitions(BrowserDriver browser, DashboardDriver dashboard)
    {
        _browser   = browser;
        _dashboard = dashboard;
        _page      = new GovernanceApprovalsPage(browser.Page);
    }

    private IPage Page => _browser.Page;

    // -------------------------------------------------------------------------
    // Then
    // -------------------------------------------------------------------------

    [Then(@"I should see the approval workflow steps")]
    public async Task ThenIShouldSeeTheApprovalWorkflowSteps()
    {
        await _page.AssertWorkflowStepsVisibleAsync();
    }

    [Then(@"I should see feature cards for approval types")]
    public async Task ThenIShouldSeeFeatureCardsForApprovalTypes()
    {
        await _page.AssertFeatureCardsVisibleAsync();
    }

    [Then(@"I should see links to other governance pages")]
    public async Task ThenIShouldSeeLinksToOtherGovernancePages()
    {
        await _page.AssertGovernanceNavigationLinksVisibleAsync();
    }
}
