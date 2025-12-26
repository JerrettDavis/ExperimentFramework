using ExperimentFramework.Science.Models.Hypothesis;
using ExperimentFramework.Science.Models.Results;

namespace ExperimentFramework.Science.Reporting;

/// <summary>
/// Represents a complete experiment report with all analysis results.
/// </summary>
public sealed class ExperimentReport
{
    /// <summary>
    /// Gets the experiment name.
    /// </summary>
    public required string ExperimentName { get; init; }

    /// <summary>
    /// Gets the hypothesis being tested.
    /// </summary>
    public HypothesisDefinition? Hypothesis { get; init; }

    /// <summary>
    /// Gets when the experiment started.
    /// </summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>
    /// Gets when the analysis was performed.
    /// </summary>
    public required DateTimeOffset AnalyzedAt { get; init; }

    /// <summary>
    /// Gets the duration of the experiment.
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Gets the status of the experiment.
    /// </summary>
    public required ExperimentStatus Status { get; init; }

    /// <summary>
    /// Gets the overall conclusion.
    /// </summary>
    public required ExperimentConclusion Conclusion { get; init; }

    /// <summary>
    /// Gets the primary analysis result.
    /// </summary>
    public StatisticalTestResult? PrimaryResult { get; init; }

    /// <summary>
    /// Gets results for secondary endpoints.
    /// </summary>
    public IReadOnlyDictionary<string, StatisticalTestResult>? SecondaryResults { get; init; }

    /// <summary>
    /// Gets effect size information.
    /// </summary>
    public EffectSizeResult? EffectSize { get; init; }

    /// <summary>
    /// Gets power analysis results.
    /// </summary>
    public PowerAnalysisResult? PowerAnalysis { get; init; }

    /// <summary>
    /// Gets sample sizes per condition.
    /// </summary>
    public required IReadOnlyDictionary<string, int> SampleSizes { get; init; }

    /// <summary>
    /// Gets summary statistics per condition.
    /// </summary>
    public IReadOnlyDictionary<string, ConditionSummary>? ConditionSummaries { get; init; }

    /// <summary>
    /// Gets any warnings or notes about the analysis.
    /// </summary>
    public IReadOnlyList<string>? Warnings { get; init; }

    /// <summary>
    /// Gets recommendations based on the analysis.
    /// </summary>
    public IReadOnlyList<string>? Recommendations { get; init; }

    /// <summary>
    /// Gets additional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Status of an experiment.
/// </summary>
public enum ExperimentStatus
{
    /// <summary>
    /// Experiment is still running, insufficient data.
    /// </summary>
    Running,

    /// <summary>
    /// Experiment has reached required sample size.
    /// </summary>
    Completed,

    /// <summary>
    /// Experiment was stopped early.
    /// </summary>
    Stopped,

    /// <summary>
    /// Analysis failed due to data issues.
    /// </summary>
    Failed
}

/// <summary>
/// Overall conclusion of the experiment.
/// </summary>
public enum ExperimentConclusion
{
    /// <summary>
    /// Cannot draw conclusion yet.
    /// </summary>
    Inconclusive,

    /// <summary>
    /// Treatment is significantly better than control.
    /// </summary>
    TreatmentWins,

    /// <summary>
    /// Control is significantly better than treatment.
    /// </summary>
    ControlWins,

    /// <summary>
    /// No significant difference found.
    /// </summary>
    NoSignificantDifference,

    /// <summary>
    /// Treatment is non-inferior to control.
    /// </summary>
    TreatmentNonInferior,

    /// <summary>
    /// Treatment is equivalent to control.
    /// </summary>
    TreatmentEquivalent
}

/// <summary>
/// Summary statistics for a condition.
/// </summary>
public sealed class ConditionSummary
{
    /// <summary>
    /// Gets the condition name.
    /// </summary>
    public required string Condition { get; init; }

    /// <summary>
    /// Gets the sample size.
    /// </summary>
    public required int SampleSize { get; init; }

    /// <summary>
    /// Gets the mean value.
    /// </summary>
    public double? Mean { get; init; }

    /// <summary>
    /// Gets the standard deviation.
    /// </summary>
    public double? StandardDeviation { get; init; }

    /// <summary>
    /// Gets the median value.
    /// </summary>
    public double? Median { get; init; }

    /// <summary>
    /// Gets the minimum value.
    /// </summary>
    public double? Minimum { get; init; }

    /// <summary>
    /// Gets the maximum value.
    /// </summary>
    public double? Maximum { get; init; }

    /// <summary>
    /// Gets the success rate (for binary outcomes).
    /// </summary>
    public double? SuccessRate { get; init; }

    /// <summary>
    /// Gets the number of successes (for binary outcomes).
    /// </summary>
    public int? SuccessCount { get; init; }
}

/// <summary>
/// Effect size result.
/// </summary>
public sealed class EffectSizeResult
{
    /// <summary>
    /// Gets the name of the effect size measure.
    /// </summary>
    public required string MeasureName { get; init; }

    /// <summary>
    /// Gets the effect size value.
    /// </summary>
    public required double Value { get; init; }

    /// <summary>
    /// Gets the magnitude interpretation.
    /// </summary>
    public required EffectSizeMagnitude Magnitude { get; init; }

    /// <summary>
    /// Gets the confidence interval lower bound.
    /// </summary>
    public double? ConfidenceIntervalLower { get; init; }

    /// <summary>
    /// Gets the confidence interval upper bound.
    /// </summary>
    public double? ConfidenceIntervalUpper { get; init; }
}

/// <summary>
/// Effect size magnitude interpretation.
/// </summary>
public enum EffectSizeMagnitude
{
    /// <summary>
    /// Negligible effect (d &lt; 0.2)
    /// </summary>
    Negligible,

    /// <summary>
    /// Small effect (0.2 ≤ d &lt; 0.5)
    /// </summary>
    Small,

    /// <summary>
    /// Medium effect (0.5 ≤ d &lt; 0.8)
    /// </summary>
    Medium,

    /// <summary>
    /// Large effect (d ≥ 0.8)
    /// </summary>
    Large
}

/// <summary>
/// Power analysis result.
/// </summary>
public sealed class PowerAnalysisResult
{
    /// <summary>
    /// Gets the achieved statistical power.
    /// </summary>
    public required double AchievedPower { get; init; }

    /// <summary>
    /// Gets the required sample size for desired power.
    /// </summary>
    public int? RequiredSampleSize { get; init; }

    /// <summary>
    /// Gets the current sample size.
    /// </summary>
    public required int CurrentSampleSize { get; init; }

    /// <summary>
    /// Gets whether the experiment is adequately powered.
    /// </summary>
    public required bool IsAdequatelyPowered { get; init; }

    /// <summary>
    /// Gets the minimum detectable effect size.
    /// </summary>
    public double? MinimumDetectableEffect { get; init; }

    /// <summary>
    /// Gets the assumed effect size used for calculations.
    /// </summary>
    public double? AssumedEffectSize { get; init; }

    /// <summary>
    /// Gets the alpha level used.
    /// </summary>
    public required double Alpha { get; init; }

    /// <summary>
    /// Gets the target power level.
    /// </summary>
    public required double TargetPower { get; init; }
}
