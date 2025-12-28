namespace ExperimentFramework.Rollout;

/// <summary>
/// Provides identity information for rollout allocation decisions.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to provide the user or session identity that will be
/// used to determine rollout allocation. The same identity should always be returned
/// for the same user to ensure consistent rollout behavior.
/// </para>
/// <para>
/// Common implementations include:
/// <list type="bullet">
/// <item><description>User ID from authentication claims</description></item>
/// <item><description>Session ID from cookies</description></item>
/// <item><description>Device ID from headers</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IRolloutIdentityProvider
{
    /// <summary>
    /// Attempts to get the current identity for rollout allocation.
    /// </summary>
    /// <param name="identity">When this method returns, contains the identity if available.</param>
    /// <returns>True if an identity was successfully obtained; otherwise false.</returns>
    /// <remarks>
    /// If no identity is available, rollouts should fall back to the default behavior
    /// (typically the control/excluded variant).
    /// </remarks>
    bool TryGetIdentity(out string identity);
}
