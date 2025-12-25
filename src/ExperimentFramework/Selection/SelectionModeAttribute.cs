namespace ExperimentFramework.Selection;

/// <summary>
/// Specifies the mode identifier for a selection mode provider.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to <see cref="ISelectionModeProvider"/> implementations
/// to declare their mode identifier. This enables the use of
/// <see cref="SelectionModeProviderFactory{TProvider}"/> without requiring
/// a custom factory implementation.
/// </para>
/// <para>
/// Example:
/// <code>
/// [SelectionMode("Redis")]
/// public class RedisSelectionProvider : SelectionModeProviderBase
/// {
///     // Implementation...
/// }
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SelectionModeAttribute : Attribute
{
    /// <summary>
    /// Gets the unique identifier for this selection mode.
    /// </summary>
    public string ModeIdentifier { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="SelectionModeAttribute"/>.
    /// </summary>
    /// <param name="modeIdentifier">
    /// The unique identifier for this selection mode.
    /// Examples: "OpenFeature", "Redis", "StickyRouting"
    /// </param>
    public SelectionModeAttribute(string modeIdentifier)
    {
        ArgumentNullException.ThrowIfNull(modeIdentifier);
        ModeIdentifier = modeIdentifier;
    }
}
