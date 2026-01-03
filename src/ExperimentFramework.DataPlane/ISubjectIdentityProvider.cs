namespace ExperimentFramework.DataPlane;

/// <summary>
/// Provides subject identity information for exposure logging.
/// </summary>
public interface ISubjectIdentityProvider
{
    /// <summary>
    /// Gets the subject type (e.g., "user", "session", "device").
    /// </summary>
    string SubjectType { get; }

    /// <summary>
    /// Attempts to get the current subject identifier.
    /// </summary>
    /// <param name="subjectId">The subject identifier if available.</param>
    /// <returns>True if a subject identifier is available; otherwise false.</returns>
    bool TryGetSubjectId(out string subjectId);
}
