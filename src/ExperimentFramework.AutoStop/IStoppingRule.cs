namespace ExperimentFramework.AutoStop;

/// <summary>
/// Defines a stopping rule that determines when an experiment should stop.
/// </summary>
public interface IStoppingRule
{
    /// <summary>
    /// Gets the name of this stopping rule.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Evaluates whether an experiment should stop based on current data.
    /// </summary>
    /// <param name="data">The experiment data to evaluate.</param>
    /// <returns>A stopping decision with reason.</returns>
    StoppingDecision Evaluate(ExperimentData data);
}

/// <summary>
/// The result of evaluating a stopping rule.
/// </summary>
public readonly record struct StoppingDecision(
    bool ShouldStop,
    string? Reason,
    string? WinningVariant = null,
    double? Confidence = null);

/// <summary>
/// Data for evaluating stopping rules.
/// </summary>
public sealed class ExperimentData
{
    /// <summary>
    /// Gets or sets the experiment name.
    /// </summary>
    public required string ExperimentName { get; init; }

    /// <summary>
    /// Gets or sets when the experiment started.
    /// </summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// Gets or sets the variant statistics.
    /// </summary>
    public required IReadOnlyList<VariantData> Variants { get; init; }
}

/// <summary>
/// Statistics for a single variant.
/// </summary>
public sealed class VariantData
{
    /// <summary>
    /// Gets or sets the variant key.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets or sets whether this is the control variant.
    /// </summary>
    public bool IsControl { get; init; }

    /// <summary>
    /// Gets or sets the total sample size.
    /// </summary>
    public long SampleSize { get; set; }

    /// <summary>
    /// Gets or sets the number of successes (conversions).
    /// </summary>
    public long Successes { get; set; }

    /// <summary>
    /// Gets the conversion rate.
    /// </summary>
    public double ConversionRate => SampleSize > 0 ? (double)Successes / SampleSize : 0.0;

    /// <summary>
    /// Gets or sets the sum of values (for continuous metrics).
    /// </summary>
    public double ValueSum { get; set; }

    /// <summary>
    /// Gets or sets the sum of squared values (for variance calculation).
    /// </summary>
    public double ValueSumSquared { get; set; }

    /// <summary>
    /// Gets the mean value.
    /// </summary>
    public double Mean => SampleSize > 0 ? ValueSum / SampleSize : 0.0;

    /// <summary>
    /// Gets the variance.
    /// </summary>
    public double Variance => SampleSize > 1
        ? (ValueSumSquared - (ValueSum * ValueSum / SampleSize)) / (SampleSize - 1)
        : 0.0;
}
