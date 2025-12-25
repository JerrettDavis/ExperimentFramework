namespace ExperimentFramework.Models;

/// <summary>
/// Concrete experiment definition for a specific service type.
/// </summary>
/// <typeparam name="TService">
/// The service interface being experimented on. This is the abstraction that will be proxied
/// and dynamically routed to different trial implementations at runtime.
/// </typeparam>
/// <remarks>
/// <para>
/// <see cref="ServiceExperimentDefinition{TService}"/> is the immutable result of the fluent
/// <see cref="ServiceExperimentBuilder{TService}"/>. It captures all configuration necessary
/// to construct a runtime <see cref="ExperimentRegistration"/>.
/// </para>
/// <para>
/// This type is intentionally simple and declarative. It does not perform selection, invocation,
/// or error handling itself; those responsibilities are handled by the experiment proxy and pipeline.
/// </para>
/// </remarks>
internal sealed class ServiceExperimentDefinition<TService> : IExperimentDefinition
    where TService : class
{
    /// <summary>
    /// Gets the service type this experiment definition applies to.
    /// </summary>
    /// <remarks>
    /// This value is used as the lookup key when resolving experiment registrations at runtime.
    /// </remarks>
    public Type ServiceType => typeof(TService);

    /// <summary>
    /// Gets the selection mode used to determine the active trial key.
    /// </summary>
    /// <remarks>
    /// The selection mode determines whether trial selection is driven by a boolean feature flag
    /// or a configuration value.
    /// </remarks>
    public required SelectionMode Mode { get; init; }

    /// <summary>
    /// Gets the mode identifier string for provider-based selection.
    /// </summary>
    /// <remarks>
    /// If not explicitly set, this is derived from <see cref="Mode"/> using
    /// <see cref="SelectionModeExtensions.ToModeIdentifier"/>.
    /// For custom modes, this must be set explicitly.
    /// </remarks>
    public string? ModeIdentifier { get; init; }

    /// <summary>
    /// Gets the selector name associated with the selection mode.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For <see cref="SelectionMode.BooleanFeatureFlag"/>, this value represents the feature flag name.
    /// </para>
    /// <para>
    /// For <see cref="SelectionMode.ConfigurationValue"/>, this value represents the configuration key
    /// whose value will be treated as the trial key.
    /// </para>
    /// </remarks>
    public required string SelectorName { get; init; }

    /// <summary>
    /// Gets the default trial key used when selection fails or no matching trial exists.
    /// </summary>
    /// <remarks>
    /// The default trial is always considered a safe fallback and is guaranteed to exist
    /// in <see cref="Trials"/>.
    /// </remarks>
    public required string DefaultKey { get; init; }

    /// <summary>
    /// Gets the mapping of trial keys to concrete implementation types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each value must be resolvable from the dependency injection container.
    /// </para>
    /// <para>
    /// Trial keys are matched using ordinal string comparison.
    /// </para>
    /// </remarks>
    public required IReadOnlyDictionary<string, Type> Trials { get; init; }

    /// <summary>
    /// Gets the policy that determines behavior when a selected trial throws an exception.
    /// </summary>
    /// <remarks>
    /// This policy controls whether the exception is immediately propagated or whether
    /// alternative trials (including the default) are attempted.
    /// </remarks>
    public required OnErrorPolicy OnErrorPolicy { get; init; }

    /// <summary>
    /// Gets the trial key to use as a fallback when <see cref="OnErrorPolicy"/> is
    /// <see cref="OnErrorPolicy.RedirectAndReplay"/>.
    /// </summary>
    /// <remarks>
    /// This value is only used when the error policy is RedirectAndReplay. For other policies, it may be null.
    /// </remarks>
    public string? FallbackTrialKey { get; init; }

    /// <summary>
    /// Gets an ordered list of trial keys to attempt as fallbacks when <see cref="OnErrorPolicy"/> is
    /// <see cref="OnErrorPolicy.RedirectAndReplayOrdered"/>.
    /// </summary>
    /// <remarks>
    /// This value is only used when the error policy is RedirectAndReplayOrdered. For other policies, it may be null.
    /// </remarks>
    public IReadOnlyList<string>? OrderedFallbackKeys { get; init; }

    /// <summary>
    /// Gets the name of the parent experiment this trial belongs to, if any.
    /// </summary>
    public string? ExperimentName { get; init; }

    /// <summary>
    /// Gets the time from which this trial becomes active.
    /// </summary>
    /// <remarks>
    /// If set, the trial will fall back to the control implementation before this time.
    /// </remarks>
    public DateTimeOffset? StartTime { get; init; }

    /// <summary>
    /// Gets the time after which this trial becomes inactive.
    /// </summary>
    /// <remarks>
    /// If set, the trial will fall back to the control implementation after this time.
    /// </remarks>
    public DateTimeOffset? EndTime { get; init; }

    /// <summary>
    /// Gets the custom predicate that determines whether this trial is active.
    /// </summary>
    /// <remarks>
    /// If the predicate returns false, the trial falls back to the control implementation.
    /// </remarks>
    public Func<IServiceProvider, bool>? ActivationPredicate { get; init; }

    /// <summary>
    /// Creates a runtime <see cref="ExperimentRegistration"/> from this definition.
    /// </summary>
    /// <param name="_">
    /// The root service provider. This parameter is unused by this implementation but is included
    /// to satisfy the <see cref="IExperimentDefinition"/> contract and allow future extensions.
    /// </param>
    /// <returns>
    /// A fully populated <see cref="ExperimentRegistration"/> suitable for runtime use.
    /// </returns>
    /// <remarks>
    /// The returned registration is intended to be immutable and safely cached for the lifetime
    /// of the application.
    /// </remarks>
    public ExperimentRegistration CreateRegistration(IServiceProvider _)
        => new()
        {
            ServiceType = typeof(TService),
            Mode = Mode,
            ModeIdentifier = ModeIdentifier ?? Mode.ToModeIdentifier(),
            SelectorName = SelectorName,
            DefaultKey = DefaultKey,
            Trials = Trials,
            OnErrorPolicy = OnErrorPolicy,
            FallbackTrialKey = FallbackTrialKey,
            OrderedFallbackKeys = OrderedFallbackKeys,
            ExperimentName = ExperimentName,
            StartTime = StartTime,
            EndTime = EndTime,
            ActivationPredicate = ActivationPredicate
        };
}