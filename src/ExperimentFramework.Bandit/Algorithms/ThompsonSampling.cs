namespace ExperimentFramework.Bandit.Algorithms;

/// <summary>
/// Thompson Sampling bandit algorithm.
/// </summary>
/// <remarks>
/// Uses Bayesian probability matching - samples from the posterior distribution
/// of each arm and selects the arm with the highest sample. Naturally balances
/// exploration and exploitation.
/// </remarks>
public sealed class ThompsonSampling : IBanditAlgorithm
{
    private readonly Random _random;

    /// <summary>
    /// Creates a Thompson Sampling algorithm.
    /// </summary>
    /// <param name="seed">Optional random seed for reproducibility.</param>
    public ThompsonSampling(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <inheritdoc />
    public string Name => "ThompsonSampling";

    /// <inheritdoc />
    public int SelectArm(IReadOnlyList<ArmStatistics> arms)
    {
        if (arms.Count == 0)
            throw new ArgumentException("At least one arm is required", nameof(arms));

        var bestIndex = 0;
        var bestSample = double.MinValue;

        for (var i = 0; i < arms.Count; i++)
        {
            // Sample from Beta distribution using Box-Muller transform
            // Beta(alpha, beta) where alpha = successes + 1, beta = failures + 1
            var sample = SampleBeta(arms[i].Successes + 1, arms[i].Failures + 1);

            if (sample > bestSample)
            {
                bestSample = sample;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    /// <inheritdoc />
    public void UpdateArm(ArmStatistics arm, double reward)
    {
        arm.Pulls++;
        arm.TotalReward += reward;

        // Update Beta distribution parameters
        if (reward > 0.5) // Treat as success
        {
            arm.Successes++;
        }
        else
        {
            arm.Failures++;
        }
    }

    /// <summary>
    /// Samples from a Beta distribution using a simple approximation.
    /// </summary>
    private double SampleBeta(long alpha, long beta)
    {
        // Use gamma distribution sampling for Beta
        // Beta(a,b) = Gamma(a) / (Gamma(a) + Gamma(b))
        var x = SampleGamma(alpha);
        var y = SampleGamma(beta);
        return x / (x + y);
    }

    /// <summary>
    /// Samples from a Gamma distribution using Marsaglia and Tsang's method.
    /// </summary>
    private double SampleGamma(long shape)
    {
        if (shape < 1)
            return SampleGamma(shape + 1) * Math.Pow(_random.NextDouble(), 1.0 / shape);

        var d = shape - 1.0 / 3.0;
        var c = 1.0 / Math.Sqrt(9.0 * d);

        while (true)
        {
            double x, v;
            do
            {
                x = SampleNormal();
                v = 1.0 + c * x;
            } while (v <= 0);

            v = v * v * v;
            var u = _random.NextDouble();

            if (u < 1.0 - 0.0331 * x * x * x * x)
                return d * v;

            if (Math.Log(u) < 0.5 * x * x + d * (1.0 - v + Math.Log(v)))
                return d * v;
        }
    }

    /// <summary>
    /// Samples from a standard normal distribution using Box-Muller.
    /// </summary>
    private double SampleNormal()
    {
        var u1 = _random.NextDouble();
        var u2 = _random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
