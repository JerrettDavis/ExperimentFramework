namespace ExperimentFramework.AutoStop.Rules;

/// <summary>
/// Stopping rule that ensures a minimum sample size is reached.
/// </summary>
public sealed class MinimumSampleSizeRule : IStoppingRule
{
    private readonly long _minimumSamples;

    /// <summary>
    /// Creates a minimum sample size rule.
    /// </summary>
    /// <param name="minimumSamples">The minimum samples required per variant.</param>
    public MinimumSampleSizeRule(long minimumSamples = 1000)
    {
        _minimumSamples = minimumSamples;
    }

    /// <inheritdoc />
    public string Name => "MinimumSampleSize";

    /// <inheritdoc />
    public StoppingDecision Evaluate(ExperimentData data)
    {
        var allReached = data.Variants.All(v => v.SampleSize >= _minimumSamples);

        if (!allReached)
        {
            var minSamples = data.Variants.Min(v => v.SampleSize);
            return new StoppingDecision(
                false,
                $"Minimum sample size not reached (current min: {minSamples}, required: {_minimumSamples})");
        }

        return new StoppingDecision(true, "Minimum sample size reached");
    }
}
