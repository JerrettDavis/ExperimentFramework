using ExperimentFramework.Science.Models.Hypothesis;
using ExperimentFramework.Science.Reporting;

namespace ExperimentFramework.Science.Analysis;

/// <summary>
/// Defines the contract for analyzing experiment results.
/// </summary>
/// <remarks>
/// The experiment analyzer orchestrates the full analysis pipeline:
/// <list type="bullet">
/// <item><description>Retrieves outcome data from the store</description></item>
/// <item><description>Performs statistical tests appropriate to the hypothesis</description></item>
/// <item><description>Calculates effect sizes and confidence intervals</description></item>
/// <item><description>Applies multiple comparison corrections if needed</description></item>
/// <item><description>Performs power analysis</description></item>
/// <item><description>Generates a comprehensive report</description></item>
/// </list>
/// </remarks>
public interface IExperimentAnalyzer
{
    /// <summary>
    /// Analyzes an experiment and generates a report.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="options">Analysis options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The experiment report.</returns>
    Task<ExperimentReport> AnalyzeAsync(
        string experimentName,
        AnalysisOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes an experiment with a pre-defined hypothesis.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="hypothesis">The hypothesis definition.</param>
    /// <param name="options">Analysis options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The experiment report.</returns>
    Task<ExperimentReport> AnalyzeAsync(
        string experimentName,
        HypothesisDefinition hypothesis,
        AnalysisOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for experiment analysis.
/// </summary>
public sealed class AnalysisOptions
{
    /// <summary>
    /// Gets or sets the significance level (alpha).
    /// </summary>
    public double Alpha { get; set; } = 0.05;

    /// <summary>
    /// Gets or sets the target power for power analysis.
    /// </summary>
    public double TargetPower { get; set; } = 0.80;

    /// <summary>
    /// Gets or sets whether to apply multiple comparison correction.
    /// </summary>
    public bool ApplyMultipleComparisonCorrection { get; set; } = true;

    /// <summary>
    /// Gets or sets the multiple comparison correction method.
    /// </summary>
    public MultipleComparisonMethod CorrectionMethod { get; set; } = MultipleComparisonMethod.BenjaminiHochberg;

    /// <summary>
    /// Gets or sets whether to calculate effect sizes.
    /// </summary>
    public bool CalculateEffectSize { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to perform power analysis.
    /// </summary>
    public bool PerformPowerAnalysis { get; set; } = true;

    /// <summary>
    /// Gets or sets the metric to analyze (null for primary/all).
    /// </summary>
    public string? MetricName { get; set; }

    /// <summary>
    /// Gets or sets the control condition name.
    /// </summary>
    public string ControlCondition { get; set; } = "control";

    /// <summary>
    /// Gets or sets specific treatment conditions to analyze (null for all).
    /// </summary>
    public IReadOnlyList<string>? TreatmentConditions { get; set; }

    /// <summary>
    /// Gets or sets whether to include warnings in the report.
    /// </summary>
    public bool IncludeWarnings { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to generate recommendations.
    /// </summary>
    public bool GenerateRecommendations { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum sample size per group for analysis.
    /// </summary>
    public int MinimumSampleSize { get; set; } = 10;
}

/// <summary>
/// Available multiple comparison correction methods.
/// </summary>
public enum MultipleComparisonMethod
{
    /// <summary>
    /// No correction.
    /// </summary>
    None,

    /// <summary>
    /// Bonferroni correction (conservative, controls FWER).
    /// </summary>
    Bonferroni,

    /// <summary>
    /// Holm-Bonferroni correction (less conservative than Bonferroni, controls FWER).
    /// </summary>
    HolmBonferroni,

    /// <summary>
    /// Benjamini-Hochberg procedure (controls FDR, more power).
    /// </summary>
    BenjaminiHochberg
}
