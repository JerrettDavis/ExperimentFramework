namespace ExperimentFramework.Configuration.Models;

/// <summary>
/// Configuration for a single trial (service experiment).
/// </summary>
public sealed class TrialConfig
{
    /// <summary>
    /// The service interface type name.
    /// Can be simple ("IMyService") or fully qualified ("MyApp.Services.IMyService, MyApp").
    /// </summary>
    public required string ServiceType { get; set; }

    /// <summary>
    /// Selection mode configuration.
    /// </summary>
    public required SelectionModeConfig SelectionMode { get; set; }

    /// <summary>
    /// Control (default/baseline) implementation.
    /// </summary>
    public required ConditionConfig Control { get; set; }

    /// <summary>
    /// Alternative conditions/variants.
    /// </summary>
    public List<ConditionConfig>? Conditions { get; set; }

    /// <summary>
    /// Error handling policy.
    /// </summary>
    public ErrorPolicyConfig? ErrorPolicy { get; set; }

    /// <summary>
    /// Time-based activation settings.
    /// </summary>
    public ActivationConfig? Activation { get; set; }
}
