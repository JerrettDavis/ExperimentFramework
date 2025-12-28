namespace ExperimentFramework.Bandit.Algorithms;

/// <summary>
/// Epsilon-greedy bandit algorithm.
/// </summary>
/// <remarks>
/// With probability epsilon, selects a random arm (exploration).
/// Otherwise, selects the arm with the highest average reward (exploitation).
/// </remarks>
public sealed class EpsilonGreedy : IBanditAlgorithm
{
    private readonly double _epsilon;
    private readonly Random _random;

    /// <summary>
    /// Creates an epsilon-greedy algorithm.
    /// </summary>
    /// <param name="epsilon">The exploration probability (0 to 1, default 0.1).</param>
    /// <param name="seed">Optional random seed for reproducibility.</param>
    public EpsilonGreedy(double epsilon = 0.1, int? seed = null)
    {
        if (epsilon < 0 || epsilon > 1)
            throw new ArgumentOutOfRangeException(nameof(epsilon), "Epsilon must be between 0 and 1");

        _epsilon = epsilon;
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <inheritdoc />
    public string Name => "EpsilonGreedy";

    /// <inheritdoc />
    public int SelectArm(IReadOnlyList<ArmStatistics> arms)
    {
        if (arms.Count == 0)
            throw new ArgumentException("At least one arm is required", nameof(arms));

        // Explore: pick a random arm
        if (_random.NextDouble() < _epsilon)
        {
            return _random.Next(arms.Count);
        }

        // Exploit: pick the best arm
        var bestIndex = 0;
        var bestReward = arms[0].AverageReward;

        for (var i = 1; i < arms.Count; i++)
        {
            if (arms[i].AverageReward > bestReward)
            {
                bestReward = arms[i].AverageReward;
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
    }
}
