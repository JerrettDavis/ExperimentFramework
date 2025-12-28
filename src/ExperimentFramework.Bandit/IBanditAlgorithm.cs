namespace ExperimentFramework.Bandit;

/// <summary>
/// Interface for multi-armed bandit algorithms.
/// </summary>
/// <remarks>
/// Bandit algorithms adaptively allocate traffic to variants based on
/// observed performance, balancing exploration and exploitation.
/// </remarks>
public interface IBanditAlgorithm
{
    /// <summary>
    /// Gets the algorithm name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Selects an arm (variant) based on the current state.
    /// </summary>
    /// <param name="arms">The available arms with their reward statistics.</param>
    /// <returns>The index of the selected arm.</returns>
    int SelectArm(IReadOnlyList<ArmStatistics> arms);

    /// <summary>
    /// Updates the statistics for an arm after observing a reward.
    /// </summary>
    /// <param name="arm">The arm statistics to update.</param>
    /// <param name="reward">The observed reward (typically 0 or 1).</param>
    void UpdateArm(ArmStatistics arm, double reward);
}

/// <summary>
/// Statistics for a bandit arm (variant).
/// </summary>
public sealed class ArmStatistics
{
    /// <summary>
    /// Gets or sets the arm key (variant identifier).
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets or sets the number of times this arm was selected.
    /// </summary>
    public long Pulls { get; set; }

    /// <summary>
    /// Gets or sets the sum of rewards for this arm.
    /// </summary>
    public double TotalReward { get; set; }

    /// <summary>
    /// Gets the average reward for this arm.
    /// </summary>
    public double AverageReward => Pulls > 0 ? TotalReward / Pulls : 0.0;

    /// <summary>
    /// Gets or sets the number of successes (for Beta distribution).
    /// </summary>
    public long Successes { get; set; }

    /// <summary>
    /// Gets or sets the number of failures (for Beta distribution).
    /// </summary>
    public long Failures { get; set; }
}
