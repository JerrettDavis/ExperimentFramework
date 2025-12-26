namespace ExperimentFramework.Science.Models.Results;

/// <summary>
/// Represents the result of a statistical hypothesis test.
/// </summary>
public sealed class StatisticalTestResult
{
    /// <summary>
    /// Gets the name of the statistical test performed.
    /// </summary>
    public required string TestName { get; init; }

    /// <summary>
    /// Gets the test statistic value (e.g., t-statistic, chi-square statistic).
    /// </summary>
    public required double TestStatistic { get; init; }

    /// <summary>
    /// Gets the p-value for the test.
    /// </summary>
    /// <remarks>
    /// The p-value represents the probability of observing results at least as extreme
    /// as those observed, assuming the null hypothesis is true.
    /// </remarks>
    public required double PValue { get; init; }

    /// <summary>
    /// Gets the significance level (alpha) used for the test.
    /// </summary>
    public required double Alpha { get; init; }

    /// <summary>
    /// Gets whether the result is statistically significant at the given alpha level.
    /// </summary>
    public bool IsSignificant => PValue < Alpha;

    /// <summary>
    /// Gets the confidence level (1 - alpha).
    /// </summary>
    public double ConfidenceLevel => 1 - Alpha;

    /// <summary>
    /// Gets the lower bound of the confidence interval for the difference.
    /// </summary>
    public required double ConfidenceIntervalLower { get; init; }

    /// <summary>
    /// Gets the upper bound of the confidence interval for the difference.
    /// </summary>
    public required double ConfidenceIntervalUpper { get; init; }

    /// <summary>
    /// Gets the point estimate of the difference (e.g., difference in means).
    /// </summary>
    public required double PointEstimate { get; init; }

    /// <summary>
    /// Gets the degrees of freedom for the test (if applicable).
    /// </summary>
    public double? DegreesOfFreedom { get; init; }

    /// <summary>
    /// Gets the sample sizes used in the test.
    /// </summary>
    public required IReadOnlyDictionary<string, int> SampleSizes { get; init; }

    /// <summary>
    /// Gets the type of alternative hypothesis tested.
    /// </summary>
    public AlternativeHypothesisType AlternativeType { get; init; } = AlternativeHypothesisType.TwoSided;

    /// <summary>
    /// Gets additional test-specific details.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Details { get; init; }

    /// <inheritdoc />
    public override string ToString() =>
        $"{TestName}: t={TestStatistic:F4}, p={PValue:F4}, {(IsSignificant ? "significant" : "not significant")} at α={Alpha}";
}

/// <summary>
/// The type of alternative hypothesis in a statistical test.
/// </summary>
public enum AlternativeHypothesisType
{
    /// <summary>
    /// Two-sided test: H1: μ1 ≠ μ2
    /// </summary>
    TwoSided,

    /// <summary>
    /// One-sided test: H1: μ1 > μ2 (treatment greater than control)
    /// </summary>
    Greater,

    /// <summary>
    /// One-sided test: H1: μ1 &gt; μ2 (treatment less than control)
    /// </summary>
    Less
}
