using ExperimentFramework.E2E.Tests.Drivers;
using ExperimentFramework.E2E.Tests.PageObjects;
using Microsoft.Playwright;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.StepDefinitions.Targeting;

[Binding]
public class TargetingStepDefinitions
{
    private readonly BrowserDriver _browser;
    private readonly DashboardDriver _dashboard;
    private readonly TargetingPage _page;

    public TargetingStepDefinitions(BrowserDriver browser, DashboardDriver dashboard)
    {
        _browser   = browser;
        _dashboard = dashboard;
        _page      = new TargetingPage(browser.Page);
    }

    private IPage Page => _browser.Page;

    // -------------------------------------------------------------------------
    // Given
    // -------------------------------------------------------------------------

    [Given(@"I am on the targeting rules page")]
    public async Task GivenIAmOnTheTargetingRulesPage()
    {
        await _dashboard.NavigateToAsync("/dashboard/targeting");
        await _page.WaitForPageLoadAsync();
    }

    // -------------------------------------------------------------------------
    // When
    // -------------------------------------------------------------------------

    [When(@"I click the refresh button")]
    public async Task WhenIClickTheRefreshButton()
    {
        await _page.ClickRefreshAsync();
    }

    // -------------------------------------------------------------------------
    // Then
    // -------------------------------------------------------------------------

    [Then(@"I should see the targeting rules display")]
    public async Task ThenIShouldSeeTheTargetingRulesDisplay()
    {
        await _page.AssertRulesDisplayVisibleAsync();
    }

    [Then(@"all toggle switches should be disabled")]
    public async Task ThenAllToggleSwitchesShouldBeDisabled()
    {
        await _page.AssertAllTogglesDisabledAsync();
    }

    [Then(@"each targeting rule should display condition tags")]
    public async Task ThenEachTargetingRuleShouldDisplayConditionTags()
    {
        await _page.AssertRulesHaveConditionTagsAsync();
    }

    [Then(@"each targeting rule should display a target variant")]
    public async Task ThenEachTargetingRuleShouldDisplayATargetVariant()
    {
        await _page.AssertRulesHaveTargetVariantAsync();
    }

    [Then(@"the targeting rules should reload")]
    public async Task ThenTheTargetingRulesShouldReload()
    {
        await _page.AssertRulesReloadedAsync();
    }
}
