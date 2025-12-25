namespace ExperimentFramework.StickyRouting;

/// <summary>
/// Extension methods for configuring sticky routing selection mode.
/// </summary>
public static class ExperimentBuilderExtensions
{
    /// <summary>
    /// Configures the experiment to use sticky routing for trial selection.
    /// </summary>
    /// <typeparam name="T">The service interface type.</typeparam>
    /// <param name="builder">The service experiment builder.</param>
    /// <param name="selectorName">
    /// The selector name used for consistent hashing.
    /// If not specified, uses the naming convention's FeatureFlagNameFor method.
    /// </param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This selection mode uses <see cref="IExperimentIdentityProvider"/> to get a user/session
    /// identity, then applies consistent hashing to select a trial deterministically.
    /// </para>
    /// <para>
    /// You must register an <see cref="IExperimentIdentityProvider"/> implementation in DI
    /// for this selection mode to work. If no identity is available, falls back to the default trial.
    /// </para>
    /// <para>
    /// Make sure to register the provider with <c>services.AddExperimentStickyRouting()</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Configure experiment with sticky routing
    /// .Define&lt;IRecommendationEngine&gt;(c => c
    ///     .UsingStickyRouting("RecommendationAlgorithm")
    ///     .AddDefaultTrial&lt;PopularityEngine&gt;("popularity")
    ///     .AddTrial&lt;MLEngine&gt;("ml")
    ///     .AddTrial&lt;CollaborativeEngine&gt;("collaborative"))
    /// </code>
    /// </example>
    public static ServiceExperimentBuilder<T> UsingStickyRouting<T>(
        this ServiceExperimentBuilder<T> builder,
        string? selectorName = null)
        where T : class
        => builder.UsingCustomMode(StickyRoutingModes.StickyRouting, selectorName);
}
