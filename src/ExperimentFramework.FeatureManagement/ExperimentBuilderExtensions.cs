namespace ExperimentFramework.FeatureManagement;

/// <summary>
/// Extension methods for configuring variant feature flag selection mode.
/// </summary>
public static class ExperimentBuilderExtensions
{
    /// <summary>
    /// Configures the experiment to use Microsoft.FeatureManagement variant feature flags
    /// for trial selection.
    /// </summary>
    /// <typeparam name="T">The service interface type.</typeparam>
    /// <param name="builder">The service experiment builder.</param>
    /// <param name="featureFlagName">
    /// The variant feature flag name to evaluate.
    /// If not specified, uses the naming convention's VariantFlagNameFor method.
    /// </param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This selection mode uses <c>IVariantFeatureManager.GetVariantAsync</c> to get the
    /// current variant name, which is then used as the trial key.
    /// </para>
    /// <para>
    /// Make sure to register the provider with <c>services.AddExperimentVariantFeatureFlags()</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Configure experiment with variant feature flag
    /// .Define&lt;IPaymentProcessor&gt;(c => c
    ///     .UsingVariantFeatureFlag("PaymentProviderVariant")
    ///     .AddDefaultTrial&lt;StripeProcessor&gt;("stripe")
    ///     .AddTrial&lt;PayPalProcessor&gt;("paypal")
    ///     .AddTrial&lt;SquareProcessor&gt;("square"))
    /// </code>
    /// </example>
    public static ServiceExperimentBuilder<T> UsingVariantFeatureFlag<T>(
        this ServiceExperimentBuilder<T> builder,
        string? featureFlagName = null)
        where T : class
        => builder.UsingCustomMode(VariantFeatureFlagModes.VariantFeatureFlag, featureFlagName);
}
