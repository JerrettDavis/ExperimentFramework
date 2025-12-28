namespace ExperimentFramework.Admin;

/// <summary>
/// Provides read access to registered experiments.
/// </summary>
public interface IExperimentRegistry
{
    /// <summary>
    /// Gets all registered experiments.
    /// </summary>
    /// <returns>All registered experiments.</returns>
    IEnumerable<ExperimentInfo> GetAllExperiments();

    /// <summary>
    /// Gets a specific experiment by name.
    /// </summary>
    /// <param name="name">The experiment name.</param>
    /// <returns>The experiment info, or null if not found.</returns>
    ExperimentInfo? GetExperiment(string name);
}

/// <summary>
/// Provides write access to modify experiment state at runtime.
/// </summary>
public interface IMutableExperimentRegistry : IExperimentRegistry
{
    /// <summary>
    /// Sets the active state of an experiment.
    /// </summary>
    /// <param name="name">The experiment name.</param>
    /// <param name="isActive">Whether the experiment should be active.</param>
    void SetExperimentActive(string name, bool isActive);

    /// <summary>
    /// Updates the rollout percentage for an experiment.
    /// </summary>
    /// <param name="name">The experiment name.</param>
    /// <param name="percentage">The new percentage (0-100).</param>
    void SetRolloutPercentage(string name, int percentage);
}

/// <summary>
/// Information about a registered experiment.
/// </summary>
public sealed class ExperimentInfo
{
    /// <summary>
    /// Gets or sets the experiment name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the service type being experimented on.
    /// </summary>
    public Type? ServiceType { get; init; }

    /// <summary>
    /// Gets or sets whether the experiment is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the experiment trials.
    /// </summary>
    public IReadOnlyList<TrialInfo>? Trials { get; init; }

    /// <summary>
    /// Gets or sets metadata about the experiment.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Information about a trial in an experiment.
/// </summary>
public sealed class TrialInfo
{
    /// <summary>
    /// Gets or sets the trial key.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets or sets the implementation type.
    /// </summary>
    public Type? ImplementationType { get; init; }

    /// <summary>
    /// Gets or sets whether this is the control trial.
    /// </summary>
    public bool IsControl { get; init; }
}
