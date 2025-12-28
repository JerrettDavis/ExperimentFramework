namespace ExperimentFramework.Targeting;

/// <summary>
/// Provides context information for targeting decisions.
/// </summary>
/// <remarks>
/// Implement this interface to provide user attributes, session data, or other
/// context that targeting rules can evaluate against.
/// </remarks>
public interface ITargetingContext
{
    /// <summary>
    /// Gets the unique identifier for the current user or session.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets the value of a specific attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute to retrieve.</param>
    /// <returns>The attribute value, or null if not present.</returns>
    object? GetAttribute(string attributeName);

    /// <summary>
    /// Checks if the context has a specific attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute to check.</param>
    /// <returns>True if the attribute exists; otherwise false.</returns>
    bool HasAttribute(string attributeName);

    /// <summary>
    /// Gets all attribute names present in this context.
    /// </summary>
    IEnumerable<string> AttributeNames { get; }
}
