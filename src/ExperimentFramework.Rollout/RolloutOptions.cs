namespace ExperimentFramework.Rollout;

/// <summary>
/// Configuration options for a rollout.
/// </summary>
public sealed class RolloutOptions
{
    /// <summary>
    /// Gets or sets the percentage of users who should be included in the rollout (0-100).
    /// </summary>
    /// <remarks>
    /// Users are allocated to the rollout based on a hash of their identity.
    /// A percentage of 0 means no users are included; 100 means all users.
    /// </remarks>
    public int Percentage { get; set; } = 100;

    /// <summary>
    /// Gets or sets the trial key to use when a user is included in the rollout.
    /// </summary>
    /// <remarks>
    /// When a user is included in the rollout (based on percentage), this key is selected.
    /// If null, defaults to "true".
    /// </remarks>
    public string IncludedKey { get; set; } = "true";

    /// <summary>
    /// Gets or sets the trial key to use when a user is excluded from the rollout.
    /// </summary>
    /// <remarks>
    /// When a user is not included in the rollout (based on percentage), this key is selected.
    /// If null, falls back to the control/default key.
    /// </remarks>
    public string? ExcludedKey { get; set; }

    /// <summary>
    /// Gets or sets the seed to use for consistent hashing.
    /// </summary>
    /// <remarks>
    /// Different seeds will produce different allocations for the same users.
    /// Use this to ensure different rollouts have independent allocations.
    /// </remarks>
    public string? Seed { get; set; }
}
