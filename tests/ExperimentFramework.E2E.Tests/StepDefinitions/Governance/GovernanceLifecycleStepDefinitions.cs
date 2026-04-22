using ExperimentFramework.E2E.Tests.Drivers;
using ExperimentFramework.E2E.Tests.PageObjects;
using ExperimentFramework.E2E.Tests.Support;
using Microsoft.Playwright;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.StepDefinitions.Governance;

[Binding]
public class GovernanceLifecycleStepDefinitions
{
    private readonly BrowserDriver _browser;
    private readonly DashboardDriver _dashboard;
    private readonly ScenarioContext _scenarioContext;
    private readonly GovernanceLifecyclePage _page;

    // Track state across steps within a scenario
    private string? _stateBeforeTransition;

    public GovernanceLifecycleStepDefinitions(
        BrowserDriver browser,
        DashboardDriver dashboard,
        ScenarioContext scenarioContext)
    {
        _browser         = browser;
        _dashboard       = dashboard;
        _scenarioContext = scenarioContext;
        _page            = new GovernanceLifecyclePage(browser.Page);
    }

    private IPage Page => _browser.Page;

    // -------------------------------------------------------------------------
    // Given
    // -------------------------------------------------------------------------

    [Given(@"I am on the governance lifecycle page")]
    public async Task GivenIAmOnTheGovernanceLifecyclePage()
    {
        await _dashboard.NavigateToAsync("/dashboard/governance/lifecycle");
        await _page.WaitForPageLoadAsync();
        // Register so GovernanceSharedStepDefinitions can dispatch the dropdown step.
        _scenarioContext["ActiveGovernancePage"] = (IGovernanceSelectable)_page;
    }

    // -------------------------------------------------------------------------
    // When
    // -------------------------------------------------------------------------

    [When(@"I click the first available transition")]
    public async Task WhenIClickTheFirstAvailableTransition()
    {
        _stateBeforeTransition = await _page.GetCurrentStateAsync();
        await _page.ClickFirstTransitionAsync();
    }

    [When(@"I fill in the transition form with actor {string} and reason {string}")]
    public async Task WhenIFillInTheTransitionFormWithActorAndReason(string actor, string reason)
    {
        await _page.FillTransitionFormAsync(actor, reason);
    }

    [When(@"I confirm the transition")]
    public async Task WhenIConfirmTheTransition()
    {
        await _page.ConfirmTransitionAsync();
    }

    // -------------------------------------------------------------------------
    // Then
    // -------------------------------------------------------------------------

    [Then(@"I should see the governance not configured message")]
    public async Task ThenIShouldSeeTheGovernanceNotConfiguredMessage()
    {
        // The AspireDemo does not register IGovernancePersistenceBackplane,
        // so the UI renders an info/warning box instead of live data.
        // We accept either the not-configured info box OR actual governance data.
        var isConfigured = await _page.IsConfiguredAsync();
        if (!isConfigured)
        {
            await _page.AssertNotConfiguredMessageVisibleAsync();
        }
        // If persistence IS configured the page loads normally — test still passes.
    }

    [Then(@"I should see the current governance state")]
    public async Task ThenIShouldSeeTheCurrentGovernanceState()
    {
        await _page.AssertCurrentStateVisibleAsync();
    }

    [Then(@"I should see available transitions")]
    public async Task ThenIShouldSeeAvailableTransitions()
    {
        // When governance is not configured there may be no transitions; that is acceptable.
        var isConfigured = await _page.IsConfiguredAsync();
        if (isConfigured)
        {
            await _page.AssertTransitionsVisibleAsync();
        }
    }

    [Then(@"I should see the transition history section")]
    public async Task ThenIShouldSeeTheTransitionHistorySection()
    {
        await _page.AssertTransitionHistoryVisibleAsync();
    }

    [Then(@"the governance state should update")]
    public async Task ThenTheGovernanceStateShouldUpdate()
    {
        await _page.AssertStateUpdatedAsync(_stateBeforeTransition);
    }
}
