namespace ExperimentFramework.Rollout;

/// <summary>
/// Configuration for a staged rollout that increases percentage over time.
/// </summary>
public sealed class StagedRolloutOptions
{
    /// <summary>
    /// Gets or sets the rollout stages.
    /// </summary>
    public List<RolloutStage> Stages { get; set; } = [];

    /// <summary>
    /// Gets or sets the trial key to use when a user is included in the rollout.
    /// </summary>
    public string IncludedKey { get; set; } = "true";

    /// <summary>
    /// Gets or sets the trial key to use when a user is excluded from the rollout.
    /// </summary>
    public string? ExcludedKey { get; set; }

    /// <summary>
    /// Gets or sets the seed for consistent hashing.
    /// </summary>
    public string? Seed { get; set; }

    /// <summary>
    /// Calculates the current percentage based on the stages and current time.
    /// </summary>
    /// <param name="currentTime">The current time (defaults to UTC now).</param>
    /// <returns>The current rollout percentage.</returns>
    public int GetCurrentPercentage(DateTimeOffset? currentTime = null)
    {
        var now = currentTime ?? DateTimeOffset.UtcNow;

        if (Stages.Count == 0)
            return 0;

        // Find the most recent stage that has started
        var activeStage = Stages
            .Where(s => s.StartsAt <= now)
            .OrderByDescending(s => s.StartsAt)
            .FirstOrDefault();

        return activeStage?.Percentage ?? 0;
    }
}

/// <summary>
/// Represents a single stage in a staged rollout.
/// </summary>
public sealed class RolloutStage
{
    /// <summary>
    /// Gets or sets when this stage begins.
    /// </summary>
    public DateTimeOffset StartsAt { get; set; }

    /// <summary>
    /// Gets or sets the percentage for this stage (0-100).
    /// </summary>
    public int Percentage { get; set; }

    /// <summary>
    /// Gets or sets an optional description for this stage.
    /// </summary>
    public string? Description { get; set; }
}
