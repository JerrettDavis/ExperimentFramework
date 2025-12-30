namespace ExperimentFramework.Configuration.Models;

/// <summary>
/// Root configuration model for the entire experiment framework.
/// </summary>
public sealed class ExperimentFrameworkConfigurationRoot
{
    /// <summary>
    /// Global settings for the experiment framework.
    /// </summary>
    public FrameworkSettingsConfig? Settings { get; set; }

    /// <summary>
    /// Global decorators applied to all experiments.
    /// </summary>
    public List<DecoratorConfig>? Decorators { get; set; }

    /// <summary>
    /// Additional configuration file paths to scan.
    /// </summary>
    public List<string>? ConfigurationPaths { get; set; }

    /// <summary>
    /// Standalone trials (single-service experiments).
    /// </summary>
    public List<TrialConfig>? Trials { get; set; }

    /// <summary>
    /// Named experiments with multiple trials.
    /// </summary>
    public List<ExperimentConfig>? Experiments { get; set; }

    /// <summary>
    /// Data plane configuration.
    /// </summary>
    public DataPlaneConfig? DataPlane { get; set; }

    /// <summary>
    /// Governance configuration for lifecycle, approval gates, and policies.
    /// </summary>
    public GovernanceConfig? Governance { get; set; }
}
