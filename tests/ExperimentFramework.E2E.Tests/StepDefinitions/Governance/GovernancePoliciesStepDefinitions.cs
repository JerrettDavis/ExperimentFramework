using ExperimentFramework.E2E.Tests.Drivers;
using ExperimentFramework.E2E.Tests.PageObjects;
using Microsoft.Playwright;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.StepDefinitions.Governance;

[Binding]
public class GovernancePoliciesStepDefinitions
{
    private readonly BrowserDriver _browser;
    private readonly DashboardDriver _dashboard;
    private readonly ScenarioContext _scenarioContext;
    private readonly GovernancePoliciesPage _page;

    public GovernancePoliciesStepDefinitions(
        BrowserDriver browser,
        DashboardDriver dashboard,
        ScenarioContext scenarioContext)
    {
        _browser         = browser;
        _dashboard       = dashboard;
        _scenarioContext = scenarioContext;
        _page            = new GovernancePoliciesPage(browser.Page);
    }

    private IPage Page => _browser.Page;

    // -------------------------------------------------------------------------
    // Given
    // -------------------------------------------------------------------------

    [Given(@"I am on the governance policies page")]
    public async Task GivenIAmOnTheGovernancePoliciesPage()
    {
        await _dashboard.NavigateToAsync("/dashboard/governance/policies");
        await _page.WaitForPageLoadAsync();
        _scenarioContext["SelectFirstExperiment"] = (Func<Task>)SelectFirstExperimentAsync;
    }

    // -------------------------------------------------------------------------
    // Then
    // -------------------------------------------------------------------------

    [Then(@"I should see the compliance summary or not configured message")]
    public async Task ThenIShouldSeeTheComplianceSummaryOrNotConfiguredMessage()
    {
        // When IGovernancePersistenceBackplane is not registered the page shows
        // an informational message instead of live compliance data.
        var isConfigured = await _page.IsConfiguredAsync();
        if (isConfigured)
        {
            await _page.AssertComplianceSummaryVisibleAsync();
        }
        else
        {
            await _page.AssertNotConfiguredMessageVisibleAsync();
        }
    }

    [Then(@"each policy card should show compliant or non-compliant status")]
    public async Task ThenEachPolicyCardShouldShowCompliantOrNonCompliantStatus()
    {
        var isConfigured = await _page.IsConfiguredAsync();
        if (isConfigured)
        {
            await _page.AssertPolicyCardsHaveStatusAsync();
        }
        // If not configured there are no policy cards to assert against — pass gracefully.
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task SelectFirstExperimentAsync()
    {
        var select = Page.Locator(
            "select[name*='experiment' i], [data-select='experiment'], .experiment-select");
        var options = select.Locator("option");
        var count   = await options.CountAsync();
        var idx     = count > 1 ? 1 : 0;
        var value   = await options.Nth(idx).GetAttributeAsync("value");
        if (value is not null)
            await select.SelectOptionAsync(new SelectOptionValue { Value = value });
    }
}
