namespace ExperimentFramework.Data.Models;

/// <summary>
/// Represents aggregated statistics for outcomes within a trial.
/// </summary>
/// <remarks>
/// This is used to efficiently compute descriptive statistics without
/// loading all individual outcomes into memory.
/// </remarks>
public sealed class OutcomeAggregation
{
    /// <summary>
    /// Gets the trial key these statistics are for.
    /// </summary>
    public required string TrialKey { get; init; }

    /// <summary>
    /// Gets the metric name these statistics are for.
    /// </summary>
    public required string MetricName { get; init; }

    /// <summary>
    /// Gets the total number of outcomes recorded.
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// Gets the sum of all outcome values.
    /// </summary>
    public required double Sum { get; init; }

    /// <summary>
    /// Gets the sum of squared outcome values (for variance calculation).
    /// </summary>
    public required double SumOfSquares { get; init; }

    /// <summary>
    /// Gets the minimum outcome value.
    /// </summary>
    public required double Min { get; init; }

    /// <summary>
    /// Gets the maximum outcome value.
    /// </summary>
    public required double Max { get; init; }

    /// <summary>
    /// Gets the number of successful outcomes (for binary outcomes).
    /// </summary>
    /// <remarks>
    /// Only meaningful for <see cref="OutcomeType.Binary"/> outcomes.
    /// </remarks>
    public int SuccessCount { get; init; }

    /// <summary>
    /// Gets the timestamp of the first recorded outcome.
    /// </summary>
    public DateTimeOffset? FirstTimestamp { get; init; }

    /// <summary>
    /// Gets the timestamp of the last recorded outcome.
    /// </summary>
    public DateTimeOffset? LastTimestamp { get; init; }

    /// <summary>
    /// Gets the arithmetic mean of outcome values.
    /// </summary>
    public double Mean => Count > 0 ? Sum / Count : 0;

    /// <summary>
    /// Gets the sample variance of outcome values.
    /// </summary>
    public double Variance => Count > 1
        ? (SumOfSquares - (Sum * Sum / Count)) / (Count - 1)
        : 0;

    /// <summary>
    /// Gets the sample standard deviation of outcome values.
    /// </summary>
    public double StandardDeviation => Math.Sqrt(Variance);

    /// <summary>
    /// Gets the standard error of the mean.
    /// </summary>
    public double StandardError => Count > 0 ? StandardDeviation / Math.Sqrt(Count) : 0;

    /// <summary>
    /// Gets the conversion rate (for binary outcomes).
    /// </summary>
    /// <remarks>
    /// Returns the proportion of successful outcomes (SuccessCount / Count).
    /// Only meaningful for <see cref="OutcomeType.Binary"/> outcomes.
    /// </remarks>
    public double ConversionRate => Count > 0 ? (double)SuccessCount / Count : 0;

    /// <inheritdoc />
    public override string ToString() =>
        $"Aggregation[{TrialKey}/{MetricName}] N={Count}, Mean={Mean:F4}, SD={StandardDeviation:F4}";

    /// <summary>
    /// Creates an empty aggregation for a trial/metric combination.
    /// </summary>
    public static OutcomeAggregation Empty(string trialKey, string metricName) => new()
    {
        TrialKey = trialKey,
        MetricName = metricName,
        Count = 0,
        Sum = 0,
        SumOfSquares = 0,
        Min = double.MaxValue,
        Max = double.MinValue,
        SuccessCount = 0
    };

    /// <summary>
    /// Creates a new aggregation that includes an additional outcome value.
    /// </summary>
    public OutcomeAggregation WithValue(double value, bool isSuccess = false, DateTimeOffset? timestamp = null)
    {
        var newFirst = FirstTimestamp ?? timestamp;
        var newLast = timestamp ?? LastTimestamp;

        return new OutcomeAggregation
        {
            TrialKey = TrialKey,
            MetricName = MetricName,
            Count = Count + 1,
            Sum = Sum + value,
            SumOfSquares = SumOfSquares + (value * value),
            Min = Count == 0 ? value : Math.Min(Min, value),
            Max = Count == 0 ? value : Math.Max(Max, value),
            SuccessCount = SuccessCount + (isSuccess ? 1 : 0),
            FirstTimestamp = newFirst,
            LastTimestamp = newLast
        };
    }
}
