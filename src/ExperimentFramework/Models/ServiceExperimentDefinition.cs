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
            SelectorName = SelectorName,
            DefaultKey = DefaultKey,
            Trials = Trials,
            OnErrorPolicy = OnErrorPolicy
        };
}