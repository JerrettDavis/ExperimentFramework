using ExperimentFramework.Models;
using ExperimentFramework.Naming;

namespace ExperimentFramework;

/// <summary>
/// Fluent builder used to configure an experiment for a specific service type.
/// </summary>
/// <typeparam name="TService">
/// The service interface being experimented on. This is the abstraction that will be configured
/// with an experiment proxy at registration time.
/// </typeparam>
/// <remarks>
/// <para>
/// <see cref="ServiceExperimentBuilder{TService}"/> is responsible for describing:
/// </para>
/// <list type="bullet">
/// <item><description>How a trial is selected (feature flag vs configuration value).</description></item>
/// <item><description>Which implementations participate as trials.</description></item>
/// <item><description>Which trial is considered the default.</description></item>
/// <item><description>How errors are handled when a trial fails.</description></item>
/// </list>
/// <para>
/// This builder collects configuration imperatively and is materialized into an
/// <see cref="IExperimentDefinition"/> during framework initialization.
/// </para>
/// </remarks>
public sealed class ServiceExperimentBuilder<TService>
    where TService : class
{
    private readonly Dictionary<string, Type> _trials = new(StringComparer.Ordinal);
    private string? _defaultKey;
    private SelectionMode _mode = SelectionMode.BooleanFeatureFlag;
    private string? _selectorName;
    private OnErrorPolicy _onErrorPolicy = OnErrorPolicy.Throw;

    /// <summary>
    /// Configures trial selection to use a boolean feature flag.
    /// </summary>
    /// <param name="featureName">
    /// The name of the feature flag to evaluate. If <see langword="null"/>, a default name will be
    /// derived from <typeparamref name="TService"/> using the configured naming convention.
    /// </param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// When the feature flag is enabled, the trial key <c>"true"</c> is selected.
    /// When disabled, the trial key <c>"false"</c> is selected.
    /// </para>
    /// <para>
    /// This mode is typically used for simple on/off experiments or gradual rollouts.
    /// </para>
    /// </remarks>
    public ServiceExperimentBuilder<TService> UsingFeatureFlag(string? featureName = null)
    {
        _mode = SelectionMode.BooleanFeatureFlag;
        _selectorName = featureName;
        return this;
    }

    /// <summary>
    /// Configures trial selection to use a configuration value.
    /// </summary>
    /// <param name="configKey">
    /// The configuration key whose value will be treated as the trial key.
    /// If <see langword="null"/>, a default key will be derived using
    /// the configured naming convention.
    /// </param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// The configuration value is interpreted as a string and matched directly against the
    /// registered trial keys.
    /// </para>
    /// <para>
    /// This mode is well-suited for multi-variant experiments (for example, <c>"A"</c>, <c>"B"</c>, <c>"Control"</c>).
    /// </para>
    /// </remarks>
    public ServiceExperimentBuilder<TService> UsingConfigurationKey(string? configKey = null)
    {
        _mode = SelectionMode.ConfigurationValue;
        _selectorName = configKey;
        return this;
    }

    /// <summary>
    /// Configures trial selection to use IVariantFeatureManager for variant-based selection.
    /// </summary>
    /// <param name="featureName">
    /// The feature flag name to evaluate for variant selection.
    /// If <see langword="null"/>, a default name will be derived from the service type.
    /// </param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// This mode requires the <c>Microsoft.FeatureManagement</c> package with variant support.
    /// If the variant feature manager is not available, the framework will fall back to the default trial.
    /// </para>
    /// <para>
    /// The variant name returned by the feature manager is used as the trial key.
    /// </para>
    /// </remarks>
    public ServiceExperimentBuilder<TService> UsingVariantFeatureFlag(string? featureName = null)
    {
        _mode = SelectionMode.VariantFeatureFlag;
        _selectorName = featureName;
        return this;
    }

    /// <summary>
    /// Configures trial selection to use sticky routing based on user/session identity hashing.
    /// </summary>
    /// <param name="selectorName">
    /// The selector name used as a salt for hashing (typically a feature flag name).
    /// If <see langword="null"/>, a default name will be derived from the service type.
    /// </param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// Sticky routing provides deterministic trial selection based on user/session identity.
    /// The same identity will always be routed to the same trial for true A/B testing.
    /// </para>
    /// <para>
    /// Requires <c>IExperimentIdentityProvider</c> to be registered in DI.
    /// If the identity provider is not available or returns no identity, the framework
    /// will fall back to boolean feature flag selection.
    /// </para>
    /// </remarks>
    public ServiceExperimentBuilder<TService> UsingStickyRouting(string? selectorName = null)
    {
        _mode = SelectionMode.StickyRouting;
        _selectorName = selectorName;
        return this;
    }

    /// <summary>
    /// Registers the default trial implementation for the experiment.
    /// </summary>
    /// <typeparam name="TImpl">
    /// The concrete implementation type to use as the default trial.
    /// </typeparam>
    /// <param name="key">
    /// The trial key associated with this implementation.
    /// </param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// The default trial is used when:
    /// </para>
    /// <list type="bullet">
    /// <item><description>No selector value is available.</description></item>
    /// <item><description>The selector value does not match any registered trial.</description></item>
    /// <item><description>An error policy redirects execution back to the default.</description></item>
    /// </list>
    /// </remarks>
    public ServiceExperimentBuilder<TService> AddDefaultTrial<TImpl>(string key)
        where TImpl : class, TService
    {
        _defaultKey = key;
        _trials[key] = typeof(TImpl);
        return this;
    }

    /// <summary>
    /// Registers a non-default trial implementation.
    /// </summary>
    /// <typeparam name="TImpl">
    /// The concrete implementation type to register as a trial.
    /// </typeparam>
    /// <param name="key">
    /// The trial key associated with this implementation.
    /// </param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// Trial keys must be unique within a service experiment. If a key is reused, the last
    /// registration wins.
    /// </remarks>
    public ServiceExperimentBuilder<TService> AddTrial<TImpl>(string key)
        where TImpl : class, TService
    {
        _trials[key] = typeof(TImpl);
        return this;
    }

    /// <summary>
    /// Configures the experiment to fall back to the default trial if the selected trial throws,
    /// and retry the invocation once.
    /// </summary>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// This policy is useful when introducing a risky implementation that should gracefully
    /// degrade to a known-safe default on failure.
    /// </remarks>
    public ServiceExperimentBuilder<TService> OnErrorRedirectAndReplayDefault()
    {
        _onErrorPolicy = OnErrorPolicy.RedirectAndReplayDefault;
        return this;
    }

    /// <summary>
    /// Configures the experiment to attempt remaining trials if the selected trial throws,
    /// including the default trial.
    /// </summary>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// Trials are attempted in an implementation-defined order until one succeeds.
    /// </para>
    /// <para>
    /// This policy should be used with caution, as it may result in multiple side effects
    /// if the invoked operation is not idempotent.
    /// </para>
    /// </remarks>
    public ServiceExperimentBuilder<TService> OnErrorRedirectAndReplayAny()
    {
        _onErrorPolicy = OnErrorPolicy.RedirectAndReplayAny;
        return this;
    }

    /// <summary>
    /// Builds an immutable experiment definition from the configured state.
    /// </summary>
    /// <param name="convention">
    /// The naming convention to use for deriving selector names when not explicitly specified.
    /// </param>
    /// <returns>An <see cref="IExperimentDefinition"/> describing the configured experiment.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no trials have been configured for <typeparamref name="TService"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// If no default trial key has been explicitly specified, the first registered trial
    /// is used as the default.
    /// </para>
    /// <para>
    /// If no selector name has been specified, a convention-based default is chosen
    /// based on the selection mode using the provided <paramref name="convention"/>.
    /// </para>
    /// </remarks>
    internal IExperimentDefinition Build(IExperimentNamingConvention convention)
    {
        if (_trials.Count == 0)
            throw new InvalidOperationException($"No trials were configured for {typeof(TService).FullName}.");

        _defaultKey ??= _trials.Keys.First();

        var selectorName = _selectorName ?? _mode switch
        {
            SelectionMode.BooleanFeatureFlag => convention.FeatureFlagNameFor(typeof(TService)),
            SelectionMode.VariantFeatureFlag => convention.VariantFlagNameFor(typeof(TService)),
            SelectionMode.StickyRouting => convention.FeatureFlagNameFor(typeof(TService)),
            SelectionMode.ConfigurationValue => convention.ConfigurationKeyFor(typeof(TService)),
            _ => convention.FeatureFlagNameFor(typeof(TService))
        };

        return new ServiceExperimentDefinition<TService>
        {
            Mode = _mode,
            SelectorName = selectorName,
            DefaultKey = _defaultKey,
            Trials = new Dictionary<string, Type>(_trials, StringComparer.Ordinal),
            OnErrorPolicy = _onErrorPolicy
        };
    }
}
