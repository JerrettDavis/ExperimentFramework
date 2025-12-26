using ExperimentFramework.Science.Reporting;

namespace ExperimentFramework.Science.Power;

/// <summary>
/// Defines the contract for statistical power analysis.
/// </summary>
/// <remarks>
/// Power analysis helps determine:
/// <list type="bullet">
/// <item><description>Sample size required to detect an effect of a given size</description></item>
/// <item><description>Statistical power given sample size and effect size</description></item>
/// <item><description>Minimum detectable effect size given sample size and power</description></item>
/// </list>
/// </remarks>
public interface IPowerAnalyzer
{
    /// <summary>
    /// Calculates the required sample size per group to achieve desired power.
    /// </summary>
    /// <param name="effectSize">Expected standardized effect size (Cohen's d for continuous, proportion difference for binary).</param>
    /// <param name="power">Desired statistical power (1 - β), typically 0.80 or 0.90.</param>
    /// <param name="alpha">Significance level, typically 0.05.</param>
    /// <param name="options">Additional calculation options.</param>
    /// <returns>The required sample size per group.</returns>
    int CalculateSampleSize(double effectSize, double power = 0.80, double alpha = 0.05, PowerOptions? options = null);

    /// <summary>
    /// Calculates the statistical power for a given sample size and effect size.
    /// </summary>
    /// <param name="sampleSizePerGroup">Sample size in each group.</param>
    /// <param name="effectSize">Expected standardized effect size.</param>
    /// <param name="alpha">Significance level, typically 0.05.</param>
    /// <param name="options">Additional calculation options.</param>
    /// <returns>The statistical power (probability of detecting the effect if it exists).</returns>
    double CalculatePower(int sampleSizePerGroup, double effectSize, double alpha = 0.05, PowerOptions? options = null);

    /// <summary>
    /// Calculates the minimum detectable effect size for a given sample size and power.
    /// </summary>
    /// <param name="sampleSizePerGroup">Sample size in each group.</param>
    /// <param name="power">Desired statistical power.</param>
    /// <param name="alpha">Significance level, typically 0.05.</param>
    /// <param name="options">Additional calculation options.</param>
    /// <returns>The minimum effect size that can be detected.</returns>
    double CalculateMinimumDetectableEffect(int sampleSizePerGroup, double power = 0.80, double alpha = 0.05, PowerOptions? options = null);

    /// <summary>
    /// Performs a comprehensive power analysis for an experiment.
    /// </summary>
    /// <param name="currentSampleSizePerGroup">Current sample size per group.</param>
    /// <param name="effectSize">Expected or observed effect size.</param>
    /// <param name="targetPower">Target power level.</param>
    /// <param name="alpha">Significance level.</param>
    /// <param name="options">Additional calculation options.</param>
    /// <returns>Complete power analysis result.</returns>
    PowerAnalysisResult Analyze(
        int currentSampleSizePerGroup,
        double effectSize,
        double targetPower = 0.80,
        double alpha = 0.05,
        PowerOptions? options = null);
}

/// <summary>
/// Options for power analysis calculations.
/// </summary>
public sealed class PowerOptions
{
    /// <summary>
    /// Gets or sets whether this is a one-sided (true) or two-sided (false) test.
    /// </summary>
    public bool OneSided { get; set; } = false;

    /// <summary>
    /// Gets or sets the type of outcome being analyzed.
    /// </summary>
    public PowerOutcomeType OutcomeType { get; set; } = PowerOutcomeType.Continuous;

    /// <summary>
    /// Gets or sets the baseline proportion for binary outcomes.
    /// </summary>
    public double? BaselineProportion { get; set; }

    /// <summary>
    /// Gets or sets the allocation ratio (n2/n1) for unequal group sizes.
    /// </summary>
    public double AllocationRatio { get; set; } = 1.0;
}

/// <summary>
/// Type of outcome for power calculations.
/// </summary>
public enum PowerOutcomeType
{
    /// <summary>
    /// Continuous outcome (e.g., response time, scores).
    /// </summary>
    Continuous,

    /// <summary>
    /// Binary outcome (e.g., conversion, success/failure).
    /// </summary>
    Binary
}
