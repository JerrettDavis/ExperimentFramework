using System.Reflection;

namespace ExperimentFramework.Variants;

/// <summary>
/// Reflection-based adapter for IVariantFeatureManager (avoids hard dependency).
/// </summary>
/// <remarks>
/// <para>
/// This adapter uses reflection to integrate with <c>Microsoft.FeatureManagement.IVariantFeatureManager</c>
/// without requiring a hard package dependency. The framework will gracefully degrade if the variant
/// feature manager is not available.
/// </para>
/// <para>
/// When variant feature management is unavailable, variant-based selection modes will fall back to
/// using the default trial key.
/// </para>
/// </remarks>
internal static class VariantFeatureManagerAdapter
{
    private static readonly Type? VariantFeatureManagerType;
    private static readonly MethodInfo? GetVariantAsyncMethod;

    /// <summary>
    /// Static initializer that attempts to load IVariantFeatureManager via reflection.
    /// </summary>
    static VariantFeatureManagerAdapter()
    {
        // Try to load IVariantFeatureManager via reflection
        VariantFeatureManagerType = Type.GetType(
            "Microsoft.FeatureManagement.IVariantFeatureManager, Microsoft.FeatureManagement");

        if (VariantFeatureManagerType is not null)
        {
            // Look for GetVariantAsync(string, CancellationToken)
            GetVariantAsyncMethod = VariantFeatureManagerType.GetMethod(
                "GetVariantAsync",
                [typeof(string), typeof(CancellationToken)]);
        }
    }

    /// <summary>
    /// Gets a value indicating whether IVariantFeatureManager is available at runtime.
    /// </summary>
    public static bool IsAvailable => VariantFeatureManagerType is not null && GetVariantAsyncMethod is not null;

    /// <summary>
    /// Attempts to get a variant from IVariantFeatureManager using reflection.
    /// </summary>
    /// <param name="sp">The service provider to resolve IVariantFeatureManager from.</param>
    /// <param name="featureName">The feature flag name to evaluate.</param>
    /// <param name="ct">Cancellation token for the async operation.</param>
    /// <returns>
    /// The variant name if available and successfully retrieved; otherwise <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// This method handles all reflection failures gracefully by returning <see langword="null"/>,
    /// allowing the framework to fall back to the default trial key.
    /// </remarks>
    public static async ValueTask<string?> TryGetVariantAsync(
        IServiceProvider sp,
        string featureName,
        CancellationToken ct = default)
    {
        if (!IsAvailable)
            return null;

        try
        {
            var manager = sp.GetService(VariantFeatureManagerType!);
            if (manager is null)
                return null;

            // Call GetVariantAsync(featureName, ct)
            var task = (Task?)GetVariantAsyncMethod!.Invoke(manager, [featureName, ct]);
            if (task is null)
                return null;

            await task.ConfigureAwait(false);

            // Extract variant object from Task<Variant>
            var variantProperty = task.GetType().GetProperty("Result");
            var variant = variantProperty?.GetValue(task);
            if (variant is null)
                return null;

            // Try to get variant name (standard property)
            var nameProperty = variant.GetType().GetProperty("Name");
            var name = nameProperty?.GetValue(variant) as string;

            return name;
        }
        catch
        {
            // Gracefully degrade on any reflection failures
            return null;
        }
    }
}
