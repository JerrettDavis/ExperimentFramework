using ExperimentFramework.E2E.Tests.Drivers;
using ExperimentFramework.E2E.Tests.PageObjects;
using Microsoft.Playwright;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.StepDefinitions.Governance;

/// <summary>
/// Shared step definitions used across multiple governance feature files.
/// Stores the active governance page object in <see cref="ScenarioContext"/>
/// so that individual step definition classes can retrieve it via the
/// <c>IGovernanceSelectable</c> interface.
/// </summary>
[Binding]
public class GovernanceSharedStepDefinitions
{
    private readonly BrowserDriver _browser;
    private readonly DashboardDriver _dashboard;
    private readonly ScenarioContext _scenarioContext;

    public GovernanceSharedStepDefinitions(
        BrowserDriver browser,
        DashboardDriver dashboard,
        ScenarioContext scenarioContext)
    {
        _browser         = browser;
        _dashboard       = dashboard;
        _scenarioContext = scenarioContext;
    }

    private IPage Page => _browser.Page;

    // -------------------------------------------------------------------------
    // When — shared across all governance feature files
    // -------------------------------------------------------------------------

    [When(@"I select the first experiment from the dropdown")]
    public async Task WhenISelectTheFirstExperimentFromTheDropdown()
    {
        // Dispatch to whichever page the scenario navigated to, identified by
        // the IGovernanceSelectable stored in ScenarioContext during the Given.
        if (_scenarioContext.TryGetValue<IGovernanceSelectable>("ActiveGovernancePage", out var activePage))
        {
            await activePage.SelectFirstExperimentAsync();
        }
        else
        {
            // Fallback: use a generic selector that works across all governance pages.
            var selector = _browser.Page.Locator(
                "select[data-testid*='experiment'], " +
                "select.experiment-select, " +
                "select#experimentId, " +
                "select");
            var firstOption = await selector.First.InputValueAsync();
            if (string.IsNullOrWhiteSpace(firstOption))
            {
                // Select the second option (index 1) since index 0 is often a placeholder.
                await selector.First.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            }
        }
    }
}
