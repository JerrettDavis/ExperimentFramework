namespace ExperimentFramework.OpenFeature;

/// <summary>
/// Extension methods for configuring OpenFeature selection mode.
/// </summary>
public static class ExperimentBuilderExtensions
{
    /// <summary>
    /// Configures the experiment to use OpenFeature for trial selection.
    /// </summary>
    /// <typeparam name="T">The service interface type.</typeparam>
    /// <param name="builder">The service experiment builder.</param>
    /// <param name="flagKey">
    /// The OpenFeature flag key to evaluate.
    /// If not specified, uses the naming convention's OpenFeatureFlagNameFor method.
    /// </param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This selection mode uses the OpenFeature SDK to evaluate a string flag,
    /// where the flag value is used as the trial key.
    /// </para>
    /// <para>
    /// You must configure the OpenFeature provider before using this mode:
    /// <code>
    /// await Api.Instance.SetProviderAsync(new MyFeatureFlagProvider());
    /// </code>
    /// </para>
    /// <para>
    /// Make sure to register the provider with <c>services.AddExperimentOpenFeature()</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Configure experiment with OpenFeature
    /// .Define&lt;ICheckoutFlow&gt;(c => c
    ///     .UsingOpenFeature("checkout-flow-experiment")
    ///     .AddDefaultTrial&lt;StandardCheckout&gt;("standard")
    ///     .AddTrial&lt;ExpressCheckout&gt;("express")
    ///     .AddTrial&lt;OneClickCheckout&gt;("one-click"))
    /// </code>
    /// </example>
    public static ServiceExperimentBuilder<T> UsingOpenFeature<T>(
        this ServiceExperimentBuilder<T> builder,
        string? flagKey = null)
        where T : class
        => builder.UsingCustomMode(OpenFeatureModes.OpenFeature, flagKey);
}
