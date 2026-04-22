namespace ExperimentFramework.Dashboard.Abstractions;

/// <summary>
/// Provides the ability to force a specific variant to be selected for an experiment.
/// </summary>
/// <remarks>
/// This is an optional service. When registered, the <c>ActivateVariant</c> dashboard API
/// endpoint uses it to pin a variant for diagnostic or canary purposes.
/// Implementations should integrate with the framework's selection mode override mechanism.
/// </remarks>
public interface IVariantOverrideService
{
    /// <summary>
    /// Sets the active (forced) variant for an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="variantKey">The variant key to activate.</param>
    void SetActiveVariant(string experimentName, string variantKey);

    /// <summary>
    /// Clears any forced variant for an experiment, restoring normal selection.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    void ClearActiveVariant(string experimentName);

    /// <summary>
    /// Gets the currently forced variant for an experiment, if any.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <returns>The forced variant key, or null if no override is active.</returns>
    string? GetActiveVariant(string experimentName);
}
