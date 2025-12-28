namespace ExperimentFramework.Bandit.Algorithms;

/// <summary>
/// Upper Confidence Bound (UCB1) bandit algorithm.
/// </summary>
/// <remarks>
/// Selects arms based on a confidence bound that balances exploitation
/// (average reward) with exploration (uncertainty). Arms with fewer pulls
/// have higher confidence bounds, encouraging exploration.
/// </remarks>
public sealed class UpperConfidenceBound : IBanditAlgorithm
{
    private readonly double _explorationParameter;

    /// <summary>
    /// Creates a UCB1 algorithm.
    /// </summary>
    /// <param name="explorationParameter">
    /// Controls the exploration vs exploitation trade-off.
    /// Higher values encourage more exploration. Default is sqrt(2).
    /// </param>
    public UpperConfidenceBound(double explorationParameter = 1.41421356)
    {
        _explorationParameter = explorationParameter;
    }

    /// <inheritdoc />
    public string Name => "UCB1";

    /// <inheritdoc />
    public int SelectArm(IReadOnlyList<ArmStatistics> arms)
    {
        if (arms.Count == 0)
            throw new ArgumentException("At least one arm is required", nameof(arms));

        // First, pull each arm at least once
        for (var i = 0; i < arms.Count; i++)
        {
            if (arms[i].Pulls == 0)
                return i;
        }

        // Calculate total pulls
        var totalPulls = arms.Sum(a => a.Pulls);

        // Select arm with highest UCB
        var bestIndex = 0;
        var bestUcb = CalculateUcb(arms[0], totalPulls);

        for (var i = 1; i < arms.Count; i++)
        {
            var ucb = CalculateUcb(arms[i], totalPulls);
            if (ucb > bestUcb)
            {
                bestUcb = ucb;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private double CalculateUcb(ArmStatistics arm, long totalPulls)
    {
        // UCB1 formula: average reward + sqrt(2 * ln(totalPulls) / armPulls)
        var explorationTerm = _explorationParameter * Math.Sqrt(Math.Log(totalPulls) / arm.Pulls);
        return arm.AverageReward + explorationTerm;
    }

    /// <inheritdoc />
    public void UpdateArm(ArmStatistics arm, double reward)
    {
        arm.Pulls++;
        arm.TotalReward += reward;
    }
}
