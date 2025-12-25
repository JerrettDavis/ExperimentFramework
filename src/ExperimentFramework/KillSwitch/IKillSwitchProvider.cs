namespace ExperimentFramework.KillSwitch;

/// <summary>
/// Provides kill switch functionality for experiments.
/// </summary>
public interface IKillSwitchProvider
{
    /// <summary>
    /// Checks if a specific trial is disabled by the kill switch.
    /// </summary>
    /// <param name="serviceType">The service interface type.</param>
    /// <param name="trialKey">The trial key to check.</param>
    /// <returns>True if the trial is disabled, false otherwise.</returns>
    bool IsTrialDisabled(Type serviceType, string trialKey);

    /// <summary>
    /// Checks if an entire experiment is disabled by the kill switch.
    /// </summary>
    /// <param name="serviceType">The service interface type.</param>
    /// <returns>True if the experiment is disabled, false otherwise.</returns>
    bool IsExperimentDisabled(Type serviceType);

    /// <summary>
    /// Disables a specific trial.
    /// </summary>
    /// <param name="serviceType">The service interface type.</param>
    /// <param name="trialKey">The trial key to disable.</param>
    void DisableTrial(Type serviceType, string trialKey);

    /// <summary>
    /// Disables an entire experiment (all trials fall back to default).
    /// </summary>
    /// <param name="serviceType">The service interface type.</param>
    void DisableExperiment(Type serviceType);

    /// <summary>
    /// Re-enables a specific trial.
    /// </summary>
    /// <param name="serviceType">The service interface type.</param>
    /// <param name="trialKey">The trial key to enable.</param>
    void EnableTrial(Type serviceType, string trialKey);

    /// <summary>
    /// Re-enables an entire experiment.
    /// </summary>
    /// <param name="serviceType">The service interface type.</param>
    void EnableExperiment(Type serviceType);
}

/// <summary>
/// In-memory implementation of kill switch provider.
/// </summary>
public sealed class InMemoryKillSwitchProvider : IKillSwitchProvider
{
    private readonly HashSet<string> _disabledTrials = [];
    private readonly HashSet<string> _disabledExperiments = [];
    private readonly object _lock = new();

    /// <inheritdoc />
    public bool IsTrialDisabled(Type serviceType, string trialKey)
    {
        lock (_lock)
        {
            var key = GetTrialKey(serviceType, trialKey);
            return _disabledTrials.Contains(key);
        }
    }

    /// <inheritdoc />
    public bool IsExperimentDisabled(Type serviceType)
    {
        lock (_lock)
        {
            var key = GetExperimentKey(serviceType);
            return _disabledExperiments.Contains(key);
        }
    }

    /// <inheritdoc />
    public void DisableTrial(Type serviceType, string trialKey)
    {
        lock (_lock)
        {
            var key = GetTrialKey(serviceType, trialKey);
            _disabledTrials.Add(key);
        }
    }

    /// <inheritdoc />
    public void DisableExperiment(Type serviceType)
    {
        lock (_lock)
        {
            var key = GetExperimentKey(serviceType);
            _disabledExperiments.Add(key);
        }
    }

    /// <inheritdoc />
    public void EnableTrial(Type serviceType, string trialKey)
    {
        lock (_lock)
        {
            var key = GetTrialKey(serviceType, trialKey);
            _disabledTrials.Remove(key);
        }
    }

    /// <inheritdoc />
    public void EnableExperiment(Type serviceType)
    {
        lock (_lock)
        {
            var key = GetExperimentKey(serviceType);
            _disabledExperiments.Remove(key);
        }
    }

    private static string GetTrialKey(Type serviceType, string trialKey)
        => $"{serviceType.FullName}:{trialKey}";

    private static string GetExperimentKey(Type serviceType)
        => serviceType.FullName ?? serviceType.Name;
}

/// <summary>
/// No-op implementation that never disables any experiments.
/// </summary>
public sealed class NoopKillSwitchProvider : IKillSwitchProvider
{
    /// <summary>
    /// Gets the singleton instance of the no-op kill switch provider.
    /// </summary>
    public static readonly NoopKillSwitchProvider Instance = new();

    private NoopKillSwitchProvider() { }

    /// <inheritdoc />
    public bool IsTrialDisabled(Type serviceType, string trialKey) => false;

    /// <inheritdoc />
    public bool IsExperimentDisabled(Type serviceType) => false;

    /// <inheritdoc />
    public void DisableTrial(Type serviceType, string trialKey) { }

    /// <inheritdoc />
    public void DisableExperiment(Type serviceType) { }

    /// <inheritdoc />
    public void EnableTrial(Type serviceType, string trialKey) { }

    /// <inheritdoc />
    public void EnableExperiment(Type serviceType) { }
}
