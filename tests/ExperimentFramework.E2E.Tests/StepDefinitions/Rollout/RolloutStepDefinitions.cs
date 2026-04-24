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
    private readonly ScenarioContext _scenarioContext;
    private readonly RolloutPage _page;

    // Track stage count across steps within a scenario
    private int _stageCountBeforeRemove;

    public RolloutStepDefinitions(
        BrowserDriver browser,
        DashboardDriver dashboard,
        ScenarioContext scenarioContext)
    {
        _browser         = browser;
        _dashboard       = dashboard;
        _scenarioContext = scenarioContext;
        _page            = new RolloutPage(browser.Page);
    }

    // -------------------------------------------------------------------------
    // Given
    // -------------------------------------------------------------------------

    [Given(@"I am on the rollout page")]
    public async Task GivenIAmOnTheRolloutPage()
    {
        await _dashboard.NavigateToAsync("/dashboard/rollout");
        await _page.WaitForPageLoadAsync();
        // Register the select-first-experiment delegate consumed by
        // GovernanceSharedStepDefinitions (shared across governance + rollout).
        _scenarioContext["SelectFirstExperiment"] = (Func<Task>)SelectFirstExperimentAsync;
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

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task SelectFirstExperimentAsync()
    {
        var select = _browser.Page.Locator(
            "select[name*='experiment' i], [data-select='experiment'], .experiment-select");
        var options = select.Locator("option");

        // In InteractiveServer mode the dropdown is absent during the Blazor circuit
        // reconnect phase (_loading=true). Wait for the first real option (index 1 —
        // index 0 is the placeholder) before selecting so we don't accidentally pick
        // an empty value and leave the experiment unselected — which would prevent
        // the rollout configuration panel (and its stage-name input) from rendering.
        await options.Nth(1).WaitForAsync(new LocatorWaitForOptions
        {
            State   = WaitForSelectorState.Attached,
            Timeout = 15_000,
        });

        // Wait for the Blazor Server circuit to be live before firing @onchange.
        // Without this, SelectOptionAsync may change the DOM while the SignalR
        // connection is still handshaking, in which case the server never runs
        // OnExperimentSelected and the downstream UI never renders.
        await _browser.Page.WaitForFunctionAsync(
            "() => !!(window.Blazor && window.Blazor._internal && window.Blazor._internal.navigationManager)",
            null,
            new PageWaitForFunctionOptions { Timeout = 15_000 });

        var count = await options.CountAsync();
        var idx   = count > 1 ? 1 : 0;
        var value = await options.Nth(idx).GetAttributeAsync("value");
        if (value is null) return;

        // Select and verify the side-effect (rollout-manager panel) appears on the
        // server side. If the first SelectOptionAsync raced the circuit handshake
        // and was ignored, retry up to 3 times with a short visibility probe between
        // attempts so we don't fail the whole scenario on a single lost change event.
        var panel = _browser.Page.Locator(
            ".rollout-manager, .experiment-info-panel, input[name*='stage' i][name*='name' i]");
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await select.SelectOptionAsync(new SelectOptionValue { Value = value });
            try
            {
                await panel.First.WaitForAsync(new LocatorWaitForOptions
                {
                    State   = WaitForSelectorState.Visible,
                    Timeout = attempt == 3 ? 15_000 : 5_000,
                });
                return;
            }
            catch (TimeoutException) when (attempt < 3)
            {
                // Circuit handshake likely still in flight — back off and retry.
                await Task.Delay(500);
            }
        }
    }
}
