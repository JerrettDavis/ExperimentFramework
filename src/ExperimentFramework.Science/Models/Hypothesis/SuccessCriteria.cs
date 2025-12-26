namespace ExperimentFramework.Science.Models.Hypothesis;

/// <summary>
/// Defines the criteria for declaring an experiment successful.
/// </summary>
public sealed class SuccessCriteria
{
    /// <summary>
    /// Gets the significance level (alpha) for hypothesis testing.
    /// </summary>
    /// <remarks>
    /// The probability of a Type I error (false positive).
    /// Common values: 0.05 (5%), 0.01 (1%), 0.10 (10%).
    /// </remarks>
    public double Alpha { get; init; } = 0.05;

    /// <summary>
    /// Gets the desired statistical power (1 - beta).
    /// </summary>
    /// <remarks>
    /// The probability of detecting a true effect.
    /// Common values: 0.80 (80%), 0.90 (90%).
    /// </remarks>
    public double Power { get; init; } = 0.80;

    /// <summary>
    /// Gets the minimum sample size per group required before analysis.
    /// </summary>
    public int? MinimumSampleSize { get; init; }

    /// <summary>
    /// Gets the minimum effect size to be considered practically significant.
    /// </summary>
    /// <remarks>
    /// For binary outcomes, this is typically the difference in proportions.
    /// For continuous outcomes, this may be Cohen's d or absolute difference.
    /// </remarks>
    public double? MinimumEffectSize { get; init; }

    /// <summary>
    /// Gets the non-inferiority margin for non-inferiority tests.
    /// </summary>
    /// <remarks>
    /// The maximum acceptable difference in favor of control.
    /// Treatment must be no worse than control minus this margin.
    /// </remarks>
    public double? NonInferiorityMargin { get; init; }

    /// <summary>
    /// Gets the equivalence margin for equivalence tests.
    /// </summary>
    /// <remarks>
    /// The symmetric bounds within which treatments are considered equivalent.
    /// Treatment effect must fall within ±margin of control.
    /// </remarks>
    public double? EquivalenceMargin { get; init; }

    /// <summary>
    /// Gets whether to require significance on the primary endpoint only.
    /// </summary>
    /// <remarks>
    /// If true, only the primary endpoint must be significant for success.
    /// If false, success may require multiple endpoints to be significant.
    /// </remarks>
    public bool PrimaryEndpointOnly { get; init; } = true;

    /// <summary>
    /// Gets whether to apply multiple comparison correction when testing
    /// multiple endpoints.
    /// </summary>
    public bool ApplyMultipleComparisonCorrection { get; init; } = true;

    /// <summary>
    /// Gets the minimum duration the experiment must run before stopping.
    /// </summary>
    /// <remarks>
    /// Prevents stopping too early due to random fluctuations.
    /// </remarks>
    public TimeSpan? MinimumDuration { get; init; }

    /// <summary>
    /// Gets whether to require positive direction of effect, not just significance.
    /// </summary>
    /// <remarks>
    /// If true, a statistically significant negative effect is not considered success.
    /// </remarks>
    public bool RequirePositiveEffect { get; init; } = true;
}
