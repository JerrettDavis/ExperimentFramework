using ExperimentFramework.E2E.Tests.Drivers;
using ExperimentFramework.E2E.Tests.PageObjects;
using Microsoft.Playwright;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.StepDefinitions.Rollout;

[Binding]
public class RolloutStepDefinitions
{
    private readonly BrowserDriver _browser;
    private readonly DashboardDriver _dashboard;
    private readonly RolloutPage _page;

    // Track stage count across steps within a scenario
    private int _stageCountBeforeRemove;

    public RolloutStepDefinitions(BrowserDriver browser, DashboardDriver dashboard)
    {
        _browser   = browser;
        _dashboard = dashboard;
        _page      = new RolloutPage(browser.Page);
    }

    private IPage Page => _browser.Page;

    // -------------------------------------------------------------------------
    // Given
    // -------------------------------------------------------------------------

    [Given(@"I am on the rollout page")]
    public async Task GivenIAmOnTheRolloutPage()
    {
        await _dashboard.NavigateToAsync("/dashboard/rollout");
        await _page.WaitForPageLoadAsync();
    }

    // -------------------------------------------------------------------------
    // When
    // -------------------------------------------------------------------------

    [When(@"I add a stage with name {string} percentage {int} and duration {int}")]
    public async Task WhenIAddAStageWithNamePercentageAndDuration(string name, int percentage, int duration)
    {
        await _page.AddStageAsync(name, percentage, duration);
    }

    [When(@"I remove the last stage")]
    public async Task WhenIRemoveTheLastStage()
    {
        _stageCountBeforeRemove = await _page.GetStageCountAsync();
        await _page.RemoveLastStageAsync();
    }

    [When(@"I select a target variant")]
    public async Task WhenISelectATargetVariant()
    {
        await _page.SelectFirstVariantAsync();
    }

    [When(@"I start the rollout")]
    public async Task WhenIStartTheRollout()
    {
        await _page.StartRolloutAsync();
    }

    // -------------------------------------------------------------------------
    // Then
    // -------------------------------------------------------------------------

    [Then(@"I should see the experiment selector dropdown")]
    public async Task ThenIShouldSeeTheExperimentSelectorDropdown()
    {
        await _page.AssertExperimentSelectorVisibleAsync();
    }

    [Then(@"I should see the rollout configuration panel")]
    public async Task ThenIShouldSeeTheRolloutConfigurationPanel()
    {
        await _page.AssertConfigurationPanelVisibleAsync();
    }

    [Then(@"I should see {int} stages configured")]
    public async Task ThenIShouldSeeStagesConfigured(int expectedCount)
    {
        var actual = await _page.GetStageCountAsync();
        if (actual != expectedCount)
        {
            throw new Exception(
                $"Expected {expectedCount} rollout stage(s) but found {actual}.");
        }
    }

    [Then(@"the stage count should decrease by 1")]
    public async Task ThenTheStageCountShouldDecreaseBy1()
    {
        var actual   = await _page.GetStageCountAsync();
        var expected = _stageCountBeforeRemove - 1;
        if (actual != expected)
        {
            throw new Exception(
                $"Expected stage count to decrease to {expected} but found {actual}.");
        }
    }

    [Then(@"the rollout should be in progress")]
    public async Task ThenTheRolloutShouldBeInProgress()
    {
        await _page.AssertRolloutInProgressAsync();
    }

    [Then(@"I should see the progress bar")]
    public async Task ThenIShouldSeeTheProgressBar()
    {
        await _page.AssertProgressBarVisibleAsync();
    }
}
