using ExperimentFramework.E2E.Tests.Drivers;
using ExperimentFramework.E2E.Tests.PageObjects;
using Microsoft.Playwright;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.StepDefinitions.Governance;

[Binding]
public class GovernanceAuditStepDefinitions
{
    private readonly BrowserDriver _browser;
    private readonly DashboardDriver _dashboard;
    private readonly ScenarioContext _scenarioContext;
    private readonly GovernanceAuditPage _page;

    public GovernanceAuditStepDefinitions(
        BrowserDriver browser,
        DashboardDriver dashboard,
        ScenarioContext scenarioContext)
    {
        _browser         = browser;
        _dashboard       = dashboard;
        _scenarioContext = scenarioContext;
        _page            = new GovernanceAuditPage(browser.Page);
    }

    private IPage Page => _browser.Page;

    // -------------------------------------------------------------------------
    // Given
    // -------------------------------------------------------------------------

    [Given(@"I am on the governance audit page")]
    public async Task GivenIAmOnTheGovernanceAuditPage()
    {
        await _dashboard.NavigateToAsync("/dashboard/governance/audit");
        await _page.WaitForPageLoadAsync();
        // Register the delegate consumed by GovernanceSharedStepDefinitions.
        _scenarioContext["SelectFirstExperiment"] = (Func<Task>)SelectFirstExperimentAsync;
    }

    // -------------------------------------------------------------------------
    // When
    // -------------------------------------------------------------------------

    [When(@"I filter by type {string}")]
    public async Task WhenIFilterByType(string type)
    {
        await _page.FilterByTypeAsync(type);
    }

    [When(@"I search audit entries for {string}")]
    public async Task WhenISearchAuditEntriesFor(string searchText)
    {
        await _page.SearchAsync(searchText);
    }

    // -------------------------------------------------------------------------
    // Then
    // -------------------------------------------------------------------------

    [Then(@"I should see audit trail entries or a not configured message")]
    public async Task ThenIShouldSeeAuditTrailEntriesOrANotConfiguredMessage()
    {
        // Governance persistence may not be registered in the AspireDemo.
        // Accept either real audit entries OR the not-configured info message.
        var isConfigured = await _page.IsConfiguredAsync();
        if (isConfigured)
        {
            await _page.AssertAuditEntriesVisibleAsync();
        }
        else
        {
            await _page.AssertNotConfiguredMessageVisibleAsync();
        }
    }

    [Then(@"only state transition entries should be shown")]
    public async Task ThenOnlyStateTransitionEntriesShouldBeShown()
    {
        await _page.AssertFilteredByTypeAsync("StateTransition");
    }

    [Then(@"the audit entries should be filtered by search text")]
    public async Task ThenTheAuditEntriesShouldBeFilteredBySearchText()
    {
        await _page.AssertSearchResultsVisibleAsync();
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Selects the first non-placeholder option from the experiment dropdown.
    /// Registered as a <c>Func&lt;Task&gt;</c> in ScenarioContext so the shared
    /// "I select the first experiment from the dropdown" step can call it.
    /// </summary>
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
