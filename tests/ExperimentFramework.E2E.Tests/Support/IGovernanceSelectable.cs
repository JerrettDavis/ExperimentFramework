namespace ExperimentFramework.E2E.Tests.Support;

/// <summary>
/// Contract for governance page objects that expose an experiment selector dropdown.
/// Allows <see cref="StepDefinitions.Governance.GovernanceSharedStepDefinitions"/> to
/// dispatch "select the first experiment" to whichever governance page is active.
/// </summary>
public interface IGovernanceSelectable
{
    /// <summary>Selects the first available experiment from the page's dropdown.</summary>
    Task SelectFirstExperimentAsync();
}
