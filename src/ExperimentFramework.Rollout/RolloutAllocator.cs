using System.Security.Cryptography;
using System.Text;

namespace ExperimentFramework.Rollout;

/// <summary>
/// Provides consistent allocation of users to rollouts based on percentage.
/// </summary>
/// <remarks>
/// Uses consistent hashing to ensure the same user always gets the same allocation
/// for a given rollout, but different allocations for different rollouts.
/// </remarks>
public static class RolloutAllocator
{
    /// <summary>
    /// Determines if an identity should be included in a rollout based on percentage.
    /// </summary>
    /// <param name="identity">The user identity (e.g., user ID, session ID).</param>
    /// <param name="rolloutName">The name of the rollout for consistent hashing.</param>
    /// <param name="percentage">The percentage of users to include (0-100).</param>
    /// <param name="seed">Optional seed for additional randomization.</param>
    /// <returns>True if the user should be included in the rollout; otherwise false.</returns>
    public static bool IsIncluded(string identity, string rolloutName, int percentage, string? seed = null)
    {
        if (percentage <= 0) return false;
        if (percentage >= 100) return true;

        var hashInput = $"{seed ?? string.Empty}:{rolloutName}:{identity}";
        var hash = ComputeHash(hashInput);
        var bucket = Math.Abs(hash % 100);

        return bucket < percentage;
    }

    /// <summary>
    /// Allocates an identity to one of several buckets for weighted distribution.
    /// </summary>
    /// <param name="identity">The user identity.</param>
    /// <param name="rolloutName">The name of the rollout.</param>
    /// <param name="weights">The weights for each bucket (should sum to 100).</param>
    /// <param name="seed">Optional seed for additional randomization.</param>
    /// <returns>The index of the allocated bucket.</returns>
    public static int AllocateBucket(string identity, string rolloutName, int[] weights, string? seed = null)
    {
        if (weights.Length == 0)
            throw new ArgumentException("At least one weight is required", nameof(weights));

        var hashInput = $"{seed ?? string.Empty}:{rolloutName}:{identity}";
        var hash = ComputeHash(hashInput);
        var bucket = Math.Abs(hash % 100);

        var cumulative = 0;
        for (var i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (bucket < cumulative)
                return i;
        }

        return weights.Length - 1;
    }

    private static int ComputeHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return BitConverter.ToInt32(hash, 0);
    }
}
