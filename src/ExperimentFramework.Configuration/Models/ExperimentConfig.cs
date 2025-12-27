namespace ExperimentFramework.Configuration.Models;

/// <summary>
/// Configuration for a named experiment with multiple trials.
/// </summary>
public sealed class ExperimentConfig
{
    /// <summary>
    /// Unique name for this experiment.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Trials within this experiment.
    /// </summary>
    public required List<TrialConfig> Trials { get; set; }

    /// <summary>
    /// Time-based activation settings (applied to all trials in addition to trial-level settings).
    /// </summary>
    public ActivationConfig? Activation { get; set; }

    /// <summary>
    /// Experiment metadata for tracking and reporting.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Scientific hypothesis configuration (requires ExperimentFramework.Science).
    /// </summary>
    public HypothesisConfig? Hypothesis { get; set; }
}
