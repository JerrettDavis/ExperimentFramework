using ExperimentFramework.Models;
using ExperimentFramework.Naming;
using ExperimentFramework.Selection;

namespace ExperimentFramework;

/// <summary>
/// Builder for configuring a named experiment that can contain multiple trials across different service interfaces.
/// </summary>
/// <remarks>
/// <para>
/// Use this builder to group related trials under a single named experiment. This enables:
/// <list type="bullet">
/// <item><description>Shared activation rules (time bounds, predicates) for all trials</description></item>
/// <item><description>Logical grouping for management and monitoring</description></item>
/// <item><description>Coordinated rollout across multiple service interfaces</description></item>
/// </list>
/// </para>
/// <para>
/// Example usage:
/// <code>
/// builder.Experiment("q1-migration", exp => exp
///     .Trial&lt;IDatabase&gt;(t => t.AddControl&lt;Local&gt;().AddCondition&lt;Cloud&gt;("cloud"))
///     .Trial&lt;ICache&gt;(t => t.AddControl&lt;Memory&gt;().AddCondition&lt;Redis&gt;("redis"))
///     .ActiveFrom(startTime)
///     .ActiveUntil(endTime));
/// </code>
/// </para>
/// </remarks>
public sealed class ExperimentBuilder
{
    private readonly string _name;
    private readonly List<IExperimentDefinition> _trialDefinitions = [];
    private DateTimeOffset? _startTime;
    private DateTimeOffset? _endTime;
    private Func<IServiceProvider, bool>? _activationPredicate;
    private Dictionary<string, object>? _metadata;
    
    // Selection mode fields
    private SelectionMode? _selectionMode;
    private string? _modeIdentifier;
    private string? _selectorName;

    internal ExperimentBuilder(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Experiment name cannot be null or whitespace.", nameof(name));

        _name = name;
    }

    /// <summary>
    /// Adds a trial for a specific service interface to this experiment.
    /// </summary>
    /// <typeparam name="TService">The service interface type that will be proxied.</typeparam>
    /// <param name="configure">A configuration action that defines the trial's control, conditions, and behavior.</param>
    /// <returns>The current builder instance for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// Each trial within an experiment represents a separate service interface being tested.
    /// The trial configuration defines which implementations are available and how they are selected.
    /// </para>
    /// <para>
    /// Time-based activation set on the trial will be combined with experiment-level activation.
    /// Both must be satisfied for the trial to be active.
    /// </para>
    /// </remarks>
    public ExperimentBuilder Trial<TService>(Action<ServiceExperimentBuilder<TService>> configure)
        where TService : class
    {
        var trialBuilder = new ServiceExperimentBuilder<TService>();
        trialBuilder.SetExperimentName(_name);
        configure(trialBuilder);
        _trialDefinitions.Add(trialBuilder);
        return this;
    }

    /// <summary>
    /// Configures all trials in this experiment to use a boolean feature flag for selection.
    /// </summary>
    /// <param name="featureName">
    /// The name of the feature flag to evaluate. If <see langword="null"/>, a default name will be
    /// derived from each service type using the configured naming convention.
    /// </param>
    /// <returns>The current builder instance for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// When the feature flag is enabled, the trial key <c>"true"</c> is selected.
    /// When disabled, the trial key <c>"false"</c> is selected.
    /// </para>
    /// <para>
    /// This mode is typically used for simple on/off experiments or gradual rollouts.
    /// All trials under this experiment will share the same feature flag configuration.
    /// </para>
    /// </remarks>
    public ExperimentBuilder UsingFeatureFlag(string? featureName = null)
    {
        _selectionMode = SelectionMode.BooleanFeatureFlag;
        _selectorName = featureName;
        return this;
    }

    /// <summary>
    /// Configures all trials in this experiment to use a configuration value for selection.
    /// </summary>
    /// <param name="configKey">
    /// The configuration key whose value will be treated as the trial key.
    /// If <see langword="null"/>, a default key will be derived from each service type
    /// using the configured naming convention.
    /// </param>
    /// <returns>The current builder instance for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// The configuration value is interpreted as a string and matched directly against the
    /// registered trial keys.
    /// </para>
    /// <para>
    /// This mode is well-suited for multi-variant experiments (for example, <c>"A"</c>, <c>"B"</c>, <c>"Control"</c>).
    /// All trials under this experiment will share the same configuration key.
    /// </para>
    /// </remarks>
    public ExperimentBuilder UsingConfigurationKey(string? configKey = null)
    {
        _selectionMode = SelectionMode.ConfigurationValue;
        _selectorName = configKey;
        return this;
    }

    /// <summary>
    /// Configures all trials in this experiment to use a custom selection mode provider.
    /// </summary>
    /// <param name="modeIdentifier">
    /// The identifier of the custom selection mode provider. This must match the
    /// <see cref="ISelectionModeProvider.ModeIdentifier"/> of a registered provider.
    /// </param>
    /// <param name="selectorName">
    /// The selector name passed to the provider (e.g., flag key, configuration key).
    /// If <see langword="null"/>, the provider's default naming convention is used.
    /// </param>
    /// <returns>The current builder instance for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// Custom modes allow external packages to extend the framework with new selection
    /// strategies without modifying the core library.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// .Experiment("payment-experiment", exp => exp
    ///     .UsingCustomMode("OpenFeature", "payment-provider")
    ///     .Trial&lt;IPayment&gt;(t => t
    ///         .AddControl&lt;Stripe&gt;()
    ///         .AddCondition&lt;PayPal&gt;("paypal")))
    /// </code>
    /// </para>
    /// <para>
    /// The provider must be registered in the <see cref="SelectionModeRegistry"/> before
    /// the experiment is invoked. All trials under this experiment will share the same custom mode.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="modeIdentifier"/> is null or empty.
    /// </exception>
    public ExperimentBuilder UsingCustomMode(string modeIdentifier, string? selectorName = null)
    {
        if (string.IsNullOrEmpty(modeIdentifier))
            throw new ArgumentNullException(nameof(modeIdentifier));

        _selectionMode = SelectionMode.Custom;
        _modeIdentifier = modeIdentifier;
        _selectorName = selectorName;
        return this;
    }

    /// <summary>
    /// Sets the time from which all trials in this experiment become active.
    /// </summary>
    /// <param name="startTime">The earliest time at which trials can activate.</param>
    /// <returns>The current builder instance for fluent chaining.</returns>
    /// <remarks>
    /// Before this time, all trials in the experiment will fall back to their control implementations.
    /// </remarks>
    public ExperimentBuilder ActiveFrom(DateTimeOffset startTime)
    {
        _startTime = startTime;
        return this;
    }

    /// <summary>
    /// Sets the time after which all trials in this experiment become inactive.
    /// </summary>
    /// <param name="endTime">The latest time at which trials can be active.</param>
    /// <returns>The current builder instance for fluent chaining.</returns>
    /// <remarks>
    /// After this time, all trials in the experiment will fall back to their control implementations.
    /// </remarks>
    public ExperimentBuilder ActiveUntil(DateTimeOffset endTime)
    {
        _endTime = endTime;
        return this;
    }

    /// <summary>
    /// Sets both start and end times for the experiment.
    /// </summary>
    /// <param name="start">The earliest time at which trials can activate.</param>
    /// <param name="end">The latest time at which trials can be active.</param>
    /// <returns>The current builder instance for fluent chaining.</returns>
    /// <remarks>
    /// This is a convenience method equivalent to calling both <see cref="ActiveFrom"/> and <see cref="ActiveUntil"/>.
    /// </remarks>
    public ExperimentBuilder ActiveDuring(DateTimeOffset start, DateTimeOffset end)
    {
        _startTime = start;
        _endTime = end;
        return this;
    }

    /// <summary>
    /// Sets a custom predicate that determines whether the experiment is active.
    /// </summary>
    /// <param name="predicate">
    /// A function that receives the service provider and returns true if the experiment should be active.
    /// </param>
    /// <returns>The current builder instance for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// The predicate is evaluated at runtime when determining trial selection.
    /// If it returns false, all trials in the experiment fall back to their controls.
    /// </para>
    /// <para>
    /// This predicate is evaluated in addition to time bounds. Both must be satisfied.
    /// </para>
    /// </remarks>
    public ExperimentBuilder ActiveWhen(Func<IServiceProvider, bool> predicate)
    {
        _activationPredicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        return this;
    }

    /// <summary>
    /// Adds metadata to this experiment for tracking and reporting purposes.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>The current builder instance for fluent chaining.</returns>
    /// <remarks>
    /// Metadata can be used to store information such as:
    /// <list type="bullet">
    /// <item><description>Owner/team information</description></item>
    /// <item><description>JIRA ticket or issue references</description></item>
    /// <item><description>Success criteria</description></item>
    /// <item><description>Rollback instructions</description></item>
    /// </list>
    /// </remarks>
    public ExperimentBuilder WithMetadata(string key, object value)
    {
        _metadata ??= new Dictionary<string, object>();
        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Builds the experiment definition and returns all trial definitions.
    /// </summary>
    internal IReadOnlyList<IExperimentDefinition> Build(IExperimentNamingConvention namingConvention)
    {
        // Apply experiment-level settings to each trial
        var builtDefinitions = new List<IExperimentDefinition>();
        foreach (var trialBuilder in _trialDefinitions)
        {
            if (trialBuilder is IExperimentDefinitionBuilder builder)
            {
                // Pass experiment-level activation settings to the trial
                if (_startTime.HasValue)
                    builder.ApplyExperimentStartTime(_startTime.Value);
                if (_endTime.HasValue)
                    builder.ApplyExperimentEndTime(_endTime.Value);
                if (_activationPredicate != null)
                    builder.ApplyExperimentPredicate(_activationPredicate);
                if (_selectionMode.HasValue)
                    builder.ApplyExperimentSelectionMode(_selectionMode.Value, _modeIdentifier, _selectorName);

                builtDefinitions.Add(builder.Build(namingConvention));
            }
            else
            {
                builtDefinitions.Add(trialBuilder);
            }
        }

        return builtDefinitions;
    }

    /// <summary>
    /// Gets the name of this experiment.
    /// </summary>
    internal string Name => _name;

    /// <summary>
    /// Gets the start time of this experiment.
    /// </summary>
    internal DateTimeOffset? StartTime => _startTime;

    /// <summary>
    /// Gets the end time of this experiment.
    /// </summary>
    internal DateTimeOffset? EndTime => _endTime;

    /// <summary>
    /// Gets the activation predicate of this experiment.
    /// </summary>
    internal Func<IServiceProvider, bool>? ActivationPredicate => _activationPredicate;

    /// <summary>
    /// Gets the metadata of this experiment.
    /// </summary>
    internal IReadOnlyDictionary<string, object>? Metadata => _metadata;
}

/// <summary>
/// Internal interface for experiment definition builders that support experiment-level settings.
/// </summary>
internal interface IExperimentDefinitionBuilder : IExperimentDefinition
{
    /// <summary>
    /// Applies an experiment-level start time to this trial.
    /// </summary>
    void ApplyExperimentStartTime(DateTimeOffset startTime);

    /// <summary>
    /// Applies an experiment-level end time to this trial.
    /// </summary>
    void ApplyExperimentEndTime(DateTimeOffset endTime);

    /// <summary>
    /// Applies an experiment-level activation predicate to this trial.
    /// </summary>
    void ApplyExperimentPredicate(Func<IServiceProvider, bool> predicate);

    /// <summary>
    /// Applies an experiment-level selection mode to this trial.
    /// </summary>
    void ApplyExperimentSelectionMode(SelectionMode mode, string? modeIdentifier, string? selectorName);

    /// <summary>
    /// Builds the experiment definition.
    /// </summary>
    IExperimentDefinition Build(IExperimentNamingConvention namingConvention);
}
