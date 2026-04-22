using ExperimentFramework.E2E.Tests.Drivers;
using ExperimentFramework.E2E.Tests.PageObjects;
using Microsoft.Playwright;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.StepDefinitions.Governance;

[Binding]
public class GovernanceVersionsStepDefinitions
{
    private readonly BrowserDriver _browser;
    private readonly DashboardDriver _dashboard;
    private readonly GovernanceVersionsPage _page;

    public GovernanceVersionsStepDefinitions(BrowserDriver browser, DashboardDriver dashboard)
    {
        _browser   = browser;
        _dashboard = dashboard;
        _page      = new GovernanceVersionsPage(browser.Page);
    }

    private IPage Page => _browser.Page;

    // -------------------------------------------------------------------------
    // Given
    // -------------------------------------------------------------------------

    [Given(@"I am on the governance versions page")]
    public async Task GivenIAmOnTheGovernanceVersionsPage()
    {
        await _dashboard.NavigateToAsync("/dashboard/governance/versions");
        await _page.WaitForPageLoadAsync();
    }

    // -------------------------------------------------------------------------
    // When
    // -------------------------------------------------------------------------

    [When(@"I click view on the first version")]
    public async Task WhenIClickViewOnTheFirstVersion()
    {
        await _page.ClickViewFirstVersionAsync();
    }

    [When(@"I close the version viewer")]
    public async Task WhenICloseTheVersionViewer()
    {
        await _page.CloseVersionViewerAsync();
    }

    // -------------------------------------------------------------------------
    // Then
    // -------------------------------------------------------------------------

    [Then(@"I should see the version history list")]
    public async Task ThenIShouldSeeTheVersionHistoryList()
    {
        await _page.AssertVersionHistoryVisibleAsync();
    }

    [Then(@"I should see the version detail modal with JSON content")]
    public async Task ThenIShouldSeeTheVersionDetailModalWithJsonContent()
    {
        await _page.AssertVersionDetailModalVisibleAsync();
    }

    [Then(@"the version viewer should be hidden")]
    public async Task ThenTheVersionViewerShouldBeHidden()
    {
        await _page.AssertVersionViewerHiddenAsync();
    }
}
