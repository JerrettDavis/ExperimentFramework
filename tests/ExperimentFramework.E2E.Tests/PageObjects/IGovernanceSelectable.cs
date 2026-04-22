namespace ExperimentFramework.E2E.Tests.PageObjects;

/// <summary>
/// Marker interface for governance page objects that support selecting
/// an experiment from a dropdown. Used by <c>GovernanceSharedStepDefinitions</c>
/// to dispatch the "I select the first experiment from the dropdown" step
/// to whichever governance page is currently active.
/// </summary>
public interface IGovernanceSelectable
{
    /// <summary>Selects the first available experiment in the page's experiment dropdown.</summary>
    Task SelectFirstExperimentAsync();
}
