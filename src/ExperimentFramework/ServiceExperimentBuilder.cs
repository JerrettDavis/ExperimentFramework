using ExperimentFramework.Models;
using ExperimentFramework.Naming;
using ExperimentFramework.Selection;

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
/// <para>
/// <b>Fluent DSL:</b> The builder provides multiple equivalent method names to support
/// different team conventions. For example, <see cref="AddCondition{TImpl}"/>,
/// <see cref="AddVariant{TImpl}"/>, and <see cref="AddTrial{TImpl}"/> are functionally
/// identical—use whichever terminology best describes your experiment scenario.
/// </para>
/// </remarks>
public sealed class ServiceExperimentBuilder<TService> : IExperimentDefinitionBuilder
    where TService : class
{
    private readonly Dictionary<string, Type> _trials = new(StringComparer.Ordinal);
    private string? _defaultKey;
    private Type? _controlType;
    private bool _hasExplicitControl;
    private SelectionMode _mode = SelectionMode.BooleanFeatureFlag;
    private string? _modeIdentifier;
    private string? _selectorName;
    private OnErrorPolicy _onErrorPolicy = OnErrorPolicy.Throw;
    private string? _fallbackTrialKey;
    private List<string>? _orderedFallbackKeys;

    // Time-based activation fields
    private DateTimeOffset? _startTime;
    private DateTimeOffset? _endTime;
    private Func<IServiceProvider, bool>? _activationPredicate;

    // Experiment-level fields (inherited from parent experiment)
    private string? _experimentName;
    private DateTimeOffset? _experimentStartTime;
    private DateTimeOffset? _experimentEndTime;
    private Func<IServiceProvider, bool>? _experimentPredicate;

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
    /// Configures trial selection to use a custom selection mode provider.
    /// </summary>
    /// <param name="modeIdentifier">
    /// The identifier of the custom selection mode provider. This must match the
    /// <see cref="ISelectionModeProvider.ModeIdentifier"/> of a registered provider.
    /// </param>
    /// <param name="selectorName">
    /// The selector name passed to the provider (e.g., flag key, configuration key).
    /// If <see langword="null"/>, the provider's default naming convention is used.
    /// </param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// Custom modes allow external packages to extend the framework with new selection
    /// strategies without modifying the core library.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// .Trial&lt;IPayment&gt;(t => t
    ///     .UsingCustomMode("OpenFeature", "payment-provider")
    ///     .AddControl&lt;Stripe&gt;()
    ///     .AddCondition&lt;PayPal&gt;("paypal"))
    /// </code>
    /// </para>
    /// <para>
    /// The provider must be registered in the <see cref="SelectionModeRegistry"/> before
    /// the experiment is invoked.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="modeIdentifier"/> is null or empty.
    /// </exception>
    public ServiceExperimentBuilder<TService> UsingCustomMode(string modeIdentifier, string? selectorName = null)
    {
        if (string.IsNullOrEmpty(modeIdentifier))
            throw new ArgumentNullException(nameof(modeIdentifier));

        _mode = SelectionMode.Custom;
        _modeIdentifier = modeIdentifier;
        _selectorName = selectorName;
        return this;
    }

    #region Control and Condition Registration (New Terminology)

    /// <summary>
    /// Registers the control (baseline) implementation for this trial.
    /// </summary>
    /// <typeparam name="TImpl">
    /// The concrete implementation type to use as the control.
    /// </typeparam>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// The control is the baseline implementation used when:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The trial is not active (outside time bounds or predicate returns false).</description></item>
    /// <item><description>No selector value is available.</description></item>
    /// <item><description>The selector value does not match any registered condition.</description></item>
    /// <item><description>An error policy redirects execution back to the control.</description></item>
    /// </list>
    /// <para>
    /// When no key is specified, the control is registered with the key "control".
    /// </para>
    /// </remarks>
    public ServiceExperimentBuilder<TService> AddControl<TImpl>()
        where TImpl : class, TService
        => AddControl<TImpl>("control");

    /// <summary>
    /// Registers the control (baseline) implementation for this trial with a specific key.
    /// </summary>
    /// <typeparam name="TImpl">
    /// The concrete implementation type to use as the control.
    /// </typeparam>
    /// <param name="key">
    /// The key identifying this control implementation.
    /// </param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// The control is the baseline implementation that serves as the fallback
    /// when conditions are not selected or when errors occur.
    /// </para>
    /// </remarks>
    public ServiceExperimentBuilder<TService> AddControl<TImpl>(string key)
        where TImpl : class, TService
    {
        _defaultKey = key;
        _controlType = typeof(TImpl);
        _hasExplicitControl = true;
        _trials[key] = typeof(TImpl);
        return this;
    }

    /// <summary>
    /// Registers a condition (alternative implementation) for this trial.
    /// </summary>
    /// <typeparam name="TImpl">
    /// The concrete implementation type to register as a condition.
    /// </typeparam>
    /// <param name="key">
    /// The key identifying this condition. This key is matched against the selector value.
    /// </param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// Conditions are alternative implementations that may be selected based on
    /// the trial's selection rule. "Condition" and "Variant" are interchangeable terms;
    /// use whichever fits your team's conventions.
    /// </para>
    /// <para>
    /// Condition keys must be unique within a trial. If a key is reused, the last
    /// registration wins.
    /// </para>
    /// </remarks>
    public ServiceExperimentBuilder<TService> AddCondition<TImpl>(string key)
        where TImpl : class, TService
    {
        _trials[key] = typeof(TImpl);
        return this;
    }

    /// <summary>
    /// Registers a variant (alternative implementation) for this trial.
    /// </summary>
    /// <typeparam name="TImpl">
    /// The concrete implementation type to register as a variant.
    /// </typeparam>
    /// <param name="key">
    /// The key identifying this variant. This key is matched against the selector value.
    /// </param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// "Variant" and "Condition" are interchangeable terms. Use whichever terminology
    /// fits your team's conventions.
    /// </para>
    /// </remarks>
    public ServiceExperimentBuilder<TService> AddVariant<TImpl>(string key)
        where TImpl : class, TService
        => AddCondition<TImpl>(key);

    #endregion

    #region Time-Based Activation

    /// <summary>
    /// Configures the trial to activate starting from the specified time.
    /// </summary>
    /// <param name="startTime">
    /// The time from which the trial becomes active. Before this time, the control is used.
    /// </param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// When the current time is before <paramref name="startTime"/>, the trial's
    /// selection logic is bypassed and the control implementation is used.
    /// </para>
    /// </remarks>
    public ServiceExperimentBuilder<TService> ActiveFrom(DateTimeOffset startTime)
    {
        _startTime = startTime;
        return this;
    }

    /// <summary>
    /// Configures the trial to deactivate after the specified time.
    /// </summary>
    /// <param name="endTime">
    /// The time after which the trial becomes inactive. After this time, the control is used.
    /// </param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// When the current time is after <paramref name="endTime"/>, the trial's
    /// selection logic is bypassed and the control implementation is used.
    /// </para>
    /// </remarks>
    public ServiceExperimentBuilder<TService> ActiveUntil(DateTimeOffset endTime)
    {
        _endTime = endTime;
        return this;
    }

    /// <summary>
    /// Configures the trial to be active only during the specified time window.
    /// </summary>
    /// <param name="startTime">
    /// The time from which the trial becomes active.
    /// </param>
    /// <param name="endTime">
    /// The time after which the trial becomes inactive.
    /// </param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// This is a convenience method equivalent to calling both
    /// <see cref="ActiveFrom"/> and <see cref="ActiveUntil"/>.
    /// </para>
    /// </remarks>
    public ServiceExperimentBuilder<TService> ActiveDuring(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        _startTime = startTime;
        _endTime = endTime;
        return this;
    }

    /// <summary>
    /// Configures the trial to activate only when the specified predicate returns true.
    /// </summary>
    /// <param name="predicate">
    /// A function that determines whether the trial is active. Receives the service provider
    /// for accessing services during evaluation.
    /// </param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// The predicate is evaluated on each invocation. Return true to allow normal
    /// trial selection, false to fall back to the control implementation.
    /// </para>
    /// <para>
    /// This can be used for dynamic activation based on:
    /// <list type="bullet">
    /// <item><description>Environment checks (production vs development)</description></item>
    /// <item><description>User segments or feature flags</description></item>
    /// <item><description>External configuration sources</description></item>
    /// <item><description>Custom business rules</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public ServiceExperimentBuilder<TService> ActiveWhen(Func<IServiceProvider, bool> predicate)
    {
        _activationPredicate = predicate;
        return this;
    }

    #endregion

    #region Alternative Method Names

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
        => AddControl<TImpl>(key);

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
        => AddCondition<TImpl>(key);

    #endregion

    #region Error Handling Policies

    /// <summary>
    /// Configures the trial to fall back to the control implementation on error.
    /// </summary>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// When the selected condition throws an exception, the framework will catch it and
    /// retry the invocation using the control implementation.
    /// </remarks>
    public ServiceExperimentBuilder<TService> OnErrorFallbackToControl()
        => OnErrorRedirectAndReplayDefault();

    /// <summary>
    /// Configures the trial to fall back to a specific condition on error.
    /// </summary>
    /// <param name="conditionKey">The condition key to fall back to.</param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// When the selected condition throws an exception, the framework will catch it and
    /// retry the invocation using the specified fallback condition.
    /// </remarks>
    public ServiceExperimentBuilder<TService> OnErrorFallbackTo(string conditionKey)
        => OnErrorRedirectAndReplay(conditionKey);

    /// <summary>
    /// Configures the trial to try conditions in a specific order on error.
    /// </summary>
    /// <param name="conditionKeys">The ordered list of condition keys to try.</param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// When the selected condition throws an exception, the framework will try each fallback
    /// condition in the specified order until one succeeds.
    /// </remarks>
    public ServiceExperimentBuilder<TService> OnErrorTryInOrder(params string[] conditionKeys)
        => OnErrorRedirectAndReplayOrdered(conditionKeys);

    /// <summary>
    /// Configures the trial to try any available condition on error.
    /// </summary>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// When the selected condition throws an exception, the framework will try all other
    /// registered conditions until one succeeds.
    /// </remarks>
    public ServiceExperimentBuilder<TService> OnErrorTryAny()
        => OnErrorRedirectAndReplayAny();

    /// <summary>
    /// Configures the trial to throw the exception without fallback.
    /// </summary>
    /// <returns>The current builder instance.</returns>
    public ServiceExperimentBuilder<TService> OnErrorThrow()
    {
        _onErrorPolicy = OnErrorPolicy.Throw;
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
    /// Configures the experiment to redirect to a specific trial if the selected trial throws.
    /// </summary>
    /// <param name="fallbackTrialKey">
    /// The trial key to use as a fallback. This trial will be invoked if the selected trial fails.
    /// </param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// This policy is useful when you want to redirect failed requests to a specific handler,
    /// such as a no-op implementation, a diagnostic handler, or a known-safe fallback.
    /// </para>
    /// <para>
    /// If the fallback trial also throws, the exception propagates to the caller.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="fallbackTrialKey"/> is null.
    /// </exception>
    public ServiceExperimentBuilder<TService> OnErrorRedirectAndReplay(string fallbackTrialKey)
    {
        if (fallbackTrialKey == null)
            throw new ArgumentNullException(nameof(fallbackTrialKey));

        _onErrorPolicy = OnErrorPolicy.RedirectAndReplay;
        _fallbackTrialKey = fallbackTrialKey;
        return this;
    }

    /// <summary>
    /// Configures the experiment to attempt an ordered list of fallback trials if the selected trial throws.
    /// </summary>
    /// <param name="orderedFallbackKeys">
    /// An ordered list of trial keys to attempt as fallbacks. Trials are attempted in the order specified
    /// until one succeeds or all fail.
    /// </param>
    /// <returns>The current builder instance.</returns>
    /// <remarks>
    /// <para>
    /// This policy provides fine-grained control over fallback behavior by specifying exactly
    /// which trials to try and in what order.
    /// </para>
    /// <para>
    /// If all fallback trials throw, the last exception propagates to the caller.
    /// </para>
    /// <para>
    /// This policy should be used with caution, as it may result in multiple side effects
    /// if the invoked operation is not idempotent.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="orderedFallbackKeys"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="orderedFallbackKeys"/> is empty.
    /// </exception>
    public ServiceExperimentBuilder<TService> OnErrorRedirectAndReplayOrdered(params string[] orderedFallbackKeys)
    {
        if (orderedFallbackKeys == null)
            throw new ArgumentNullException(nameof(orderedFallbackKeys));
        if (orderedFallbackKeys.Length == 0)
            throw new ArgumentException("At least one fallback trial key must be specified.", nameof(orderedFallbackKeys));

        _onErrorPolicy = OnErrorPolicy.RedirectAndReplayOrdered;
        _orderedFallbackKeys = new List<string>(orderedFallbackKeys);
        return this;
    }

    #endregion

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

        // For custom modes, use a placeholder if no selector name is provided.
        // The provider will use its own default naming convention at runtime.
        var selectorName = _selectorName ?? _mode switch
        {
            SelectionMode.BooleanFeatureFlag => convention.FeatureFlagNameFor(typeof(TService)),
            SelectionMode.ConfigurationValue => convention.ConfigurationKeyFor(typeof(TService)),
            SelectionMode.Custom => string.Empty, // Provider will derive at runtime
            _ => convention.FeatureFlagNameFor(typeof(TService))
        };

        return new ServiceExperimentDefinition<TService>
        {
            Mode = _mode,
            ModeIdentifier = _modeIdentifier, // Will be derived from Mode if null
            SelectorName = selectorName,
            DefaultKey = _defaultKey,
            Trials = new Dictionary<string, Type>(_trials, StringComparer.Ordinal),
            OnErrorPolicy = _onErrorPolicy,
            FallbackTrialKey = _fallbackTrialKey,
            OrderedFallbackKeys = _orderedFallbackKeys?.AsReadOnly(),
            ExperimentName = _experimentName,
            StartTime = _startTime ?? _experimentStartTime,
            EndTime = _endTime ?? _experimentEndTime,
            ActivationPredicate = CombinePredicates(_activationPredicate, _experimentPredicate)
        };
    }

    #region IExperimentDefinitionBuilder Implementation

    /// <summary>
    /// Gets the service type for this experiment definition.
    /// </summary>
    Type IExperimentDefinition.ServiceType => typeof(TService);

    /// <summary>
    /// Creates an experiment registration for DI integration.
    /// </summary>
    ExperimentRegistration IExperimentDefinition.CreateRegistration(IServiceProvider serviceProvider)
    {
        throw new InvalidOperationException(
            "ServiceExperimentBuilder must be built before creating a registration. " +
            "This is an internal error - please report it.");
    }

    /// <summary>
    /// Sets the name of the parent experiment this trial belongs to.
    /// </summary>
    /// <param name="name">The experiment name.</param>
    internal void SetExperimentName(string name)
    {
        _experimentName = name;
    }

    /// <inheritdoc />
    void IExperimentDefinitionBuilder.ApplyExperimentStartTime(DateTimeOffset startTime)
    {
        _experimentStartTime = startTime;
    }

    /// <inheritdoc />
    void IExperimentDefinitionBuilder.ApplyExperimentEndTime(DateTimeOffset endTime)
    {
        _experimentEndTime = endTime;
    }

    /// <inheritdoc />
    void IExperimentDefinitionBuilder.ApplyExperimentPredicate(Func<IServiceProvider, bool> predicate)
    {
        _experimentPredicate = predicate;
    }

    /// <inheritdoc />
    IExperimentDefinition IExperimentDefinitionBuilder.Build(IExperimentNamingConvention namingConvention)
    {
        return Build(namingConvention);
    }

    #endregion

    /// <summary>
    /// Combines trial-level and experiment-level predicates using AND logic.
    /// </summary>
    private static Func<IServiceProvider, bool>? CombinePredicates(
        Func<IServiceProvider, bool>? trialPredicate,
        Func<IServiceProvider, bool>? experimentPredicate)
    {
        if (trialPredicate == null && experimentPredicate == null)
            return null;
        if (trialPredicate == null)
            return experimentPredicate;
        if (experimentPredicate == null)
            return trialPredicate;

        // Both predicates must return true
        return sp => experimentPredicate(sp) && trialPredicate(sp);
    }
}
