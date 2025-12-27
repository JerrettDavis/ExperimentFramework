namespace ExperimentFramework.Configuration.Models;

/// <summary>
/// Configuration for experiment success criteria.
/// </summary>
public sealed class SuccessCriteriaConfig
{
    /// <summary>
    /// Significance level (Type I error rate). Default is 0.05.
    /// </summary>
    public double Alpha { get; set; } = 0.05;

    /// <summary>
    /// Statistical power (1 - Type II error rate). Default is 0.80.
    /// </summary>
    public double Power { get; set; } = 0.80;

    /// <summary>
    /// Minimum sample size per group.
    /// </summary>
    public int? MinimumSampleSize { get; set; }

    /// <summary>
    /// Minimum effect size to detect.
    /// </summary>
    public double? MinimumEffectSize { get; set; }

    /// <summary>
    /// Non-inferiority margin (for non-inferiority tests).
    /// </summary>
    public double? NonInferiorityMargin { get; set; }

    /// <summary>
    /// Equivalence margin (for equivalence tests).
    /// </summary>
    public double? EquivalenceMargin { get; set; }

    /// <summary>
    /// If true, only the primary endpoint must be significant.
    /// If false, all endpoints must be significant. Default is true.
    /// </summary>
    public bool PrimaryEndpointOnly { get; set; } = true;

    /// <summary>
    /// Whether to apply multiple comparison correction (e.g., Bonferroni).
    /// Default is true.
    /// </summary>
    public bool ApplyMultipleComparisonCorrection { get; set; } = true;

    /// <summary>
    /// Minimum experiment duration before results are considered valid.
    /// </summary>
    public TimeSpan? MinimumDuration { get; set; }

    /// <summary>
    /// If true, the effect must be in the positive direction.
    /// If false, any significant effect is acceptable. Default is true.
    /// </summary>
    public bool RequirePositiveEffect { get; set; } = true;
}
