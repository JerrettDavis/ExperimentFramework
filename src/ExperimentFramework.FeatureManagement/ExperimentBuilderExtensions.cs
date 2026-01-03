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

    /// <summary>
    /// Configures all trials in the experiment to use Microsoft.FeatureManagement variant feature flags
    /// for trial selection.
    /// </summary>
    /// <param name="builder">The experiment builder.</param>
    /// <param name="featureFlagName">
    /// The variant feature flag name to evaluate.
    /// If not specified, uses the naming convention's VariantFlagNameFor method for each service type.
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
    /// <para>
    /// All trials under this experiment will share the same variant feature flag configuration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Configure experiment with shared variant feature flag
    /// .Experiment("payment-migration", exp => exp
    ///     .UsingVariantFeatureFlag("PaymentProviderVariant")
    ///     .Trial&lt;IPaymentProcessor&gt;(t => t
    ///         .AddControl&lt;StripeProcessor&gt;()
    ///         .AddCondition&lt;PayPalProcessor&gt;("paypal"))
    ///     .Trial&lt;IPaymentLogger&gt;(t => t
    ///         .AddControl&lt;BasicLogger&gt;()
    ///         .AddCondition&lt;AdvancedLogger&gt;("paypal")))
    /// </code>
    /// </example>
    public static ExperimentBuilder UsingVariantFeatureFlag(
        this ExperimentBuilder builder,
        string? featureFlagName = null)
        => builder.UsingCustomMode(VariantFeatureFlagModes.VariantFeatureFlag, featureFlagName);
}
