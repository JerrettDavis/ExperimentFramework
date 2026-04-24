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
    private readonly ScenarioContext _scenarioContext;
    private readonly GovernanceVersionsPage _page;

    public GovernanceVersionsStepDefinitions(
        BrowserDriver browser,
        DashboardDriver dashboard,
        ScenarioContext scenarioContext)
    {
        _browser         = browser;
        _dashboard       = dashboard;
        _scenarioContext = scenarioContext;
        _page            = new GovernanceVersionsPage(browser.Page);
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
        _scenarioContext["SelectFirstExperiment"] = (Func<Task>)SelectFirstExperimentAsync;
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

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task SelectFirstExperimentAsync()
    {
        var select = Page.Locator(
            "select[name*='experiment' i], [data-select='experiment'], .experiment-select");
        var options = select.Locator("option");
        // Wait for the first real option (index 1; index 0 is the placeholder) before
        // selecting, in case the Blazor circuit is still reconnecting.
        await options.Nth(1).WaitForAsync(new LocatorWaitForOptions
        {
            State   = WaitForSelectorState.Attached,
            Timeout = 15_000,
        });
        var count   = await options.CountAsync();
        var idx     = count > 1 ? 1 : 0;
        var value   = await options.Nth(idx).GetAttributeAsync("value");
        if (value is not null)
            await select.SelectOptionAsync(new SelectOptionValue { Value = value });
    }
}
