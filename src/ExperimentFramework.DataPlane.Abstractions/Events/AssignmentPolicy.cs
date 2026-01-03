namespace ExperimentFramework.DataPlane.Abstractions.Events;

/// <summary>
/// Assignment consistency policies for experiments.
/// </summary>
public enum AssignmentPolicy
{
    /// <summary>
    /// Best-effort assignment with no guarantees.
    /// </summary>
    BestEffort,

    /// <summary>
    /// Assignment is sticky within a single session.
    /// </summary>
    SessionSticky,

    /// <summary>
    /// Assignment is sticky for a specific subject across sessions.
    /// </summary>
    SubjectSticky,

    /// <summary>
    /// Assignment is globally sticky (requires shared state).
    /// </summary>
    GloballySticky
}
