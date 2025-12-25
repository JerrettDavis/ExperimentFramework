namespace ExperimentFramework.StickyRouting;

/// <summary>
/// Provides the current user or session identity for sticky routing.
/// </summary>
/// <remarks>
/// <para>
/// Implementations should return a stable identifier that persists across requests for the same user/session.
/// Common examples include user IDs, session IDs, or client identifiers.
/// </para>
/// <para>
/// Do NOT use IP addresses or other volatile identifiers, as they may change and break routing consistency.
/// </para>
/// </remarks>
public interface IExperimentIdentityProvider
{
    /// <summary>
    /// Attempts to retrieve the current identity for sticky routing.
    /// </summary>
    /// <param name="identity">
    /// When this method returns <see langword="true"/>, contains the current identity.
    /// When this method returns <see langword="false"/>, the value is undefined.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if an identity is available; otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If this method returns <see langword="false"/> or throws an exception, the framework will
    /// fall back to default trial selection.
    /// </para>
    /// <para>
    /// Implementations must be thread-safe if registered as singleton. Consider using scoped lifetime
    /// for web applications to access per-request context (HttpContext, ClaimsPrincipal, etc.).
    /// </para>
    /// </remarks>
    bool TryGetIdentity(out string identity);
}
