using ExperimentFramework.E2E.Tests.Drivers;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.StepDefinitions.Governance;

/// <summary>
/// Shared step definitions used across all governance and rollout feature files.
/// Each page-specific Given step registers a <c>Func&lt;Task&gt;</c> delegate in
/// <see cref="ScenarioContext"/> under the key <c>"SelectFirstExperiment"</c> so
/// this single binding can dispatch correctly.
/// </summary>
[Binding]
public class GovernanceSharedStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;

    public GovernanceSharedStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    // -------------------------------------------------------------------------
    // When — single canonical binding for all governance / rollout features
    // -------------------------------------------------------------------------

    [When(@"I select the first experiment from the dropdown")]
    public async Task WhenISelectTheFirstExperimentFromTheDropdown()
    {
        if (_scenarioContext.TryGetValue<Func<Task>>("SelectFirstExperiment", out var selectAction))
        {
            await selectAction();
        }
        else
        {
            throw new InvalidOperationException(
                "No 'SelectFirstExperiment' action was registered in ScenarioContext. " +
                "Ensure the Given step for the current page sets this before calling " +
                "'I select the first experiment from the dropdown'.");
        }
    }
}
