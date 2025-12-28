using ExperimentFramework.Configuration.Extensions;
using ExperimentFramework.Rollout.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExperimentFramework.Rollout;

/// <summary>
/// Extension methods for registering rollout support.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds percentage-based rollout selection mode support to the experiment framework.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for rollout options.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the <see cref="RolloutProvider"/> which enables
    /// the <c>.UsingRollout()</c> selection mode.
    /// </para>
    /// <para>
    /// You must also register an <see cref="IRolloutIdentityProvider"/> implementation:
    /// <code>
    /// services.AddScoped&lt;IRolloutIdentityProvider, MyIdentityProvider&gt;();
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddExperimentRollout(opts => opts.Percentage = 50);
    /// services.AddScoped&lt;IRolloutIdentityProvider, UserIdProvider&gt;();
    /// services.AddExperimentFramework(builder);
    /// </code>
    /// </example>
    public static IServiceCollection AddExperimentRollout(
        this IServiceCollection services,
        Action<RolloutOptions>? configure = null)
    {
        if (configure != null)
        {
            services.Configure(configure);
        }

        services.AddSelectionModeProvider<RolloutProvider>();
        return services;
    }

    /// <summary>
    /// Adds staged rollout selection mode support to the experiment framework.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration for staged rollout options.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the <see cref="StagedRolloutProvider"/> which enables
    /// the <c>.UsingStagedRollout()</c> selection mode.
    /// </para>
    /// <para>
    /// You must also register an <see cref="IRolloutIdentityProvider"/> implementation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddExperimentStagedRollout(opts =>
    /// {
    ///     opts.Stages.Add(new RolloutStage { StartsAt = DateTimeOffset.UtcNow, Percentage = 10 });
    ///     opts.Stages.Add(new RolloutStage { StartsAt = DateTimeOffset.UtcNow.AddDays(1), Percentage = 50 });
    ///     opts.Stages.Add(new RolloutStage { StartsAt = DateTimeOffset.UtcNow.AddDays(7), Percentage = 100 });
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddExperimentStagedRollout(
        this IServiceCollection services,
        Action<StagedRolloutOptions>? configure = null)
    {
        if (configure != null)
        {
            services.Configure(configure);
        }

        services.AddSelectionModeProvider<StagedRolloutProvider>();
        return services;
    }

    /// <summary>
    /// Adds rollout configuration handlers to the experiment framework.
    /// This enables the 'rollout' and 'stagedRollout' selection mode types in configuration files.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddExperimentRolloutConfiguration();
    /// services.AddExperimentRollout();
    /// services.AddExperimentFrameworkFromConfiguration(configuration);
    /// </code>
    ///
    /// Configuration file example:
    /// <code>
    /// experimentFramework:
    ///   trials:
    ///     - serviceType: IPaymentProcessor
    ///       selectionMode:
    ///         type: rollout
    ///         percentage: 50
    ///         seed: payment-v2-experiment
    /// </code>
    /// </example>
    public static IServiceCollection AddExperimentRolloutConfiguration(this IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigurationSelectionModeHandler, RolloutSelectionModeHandler>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigurationSelectionModeHandler, StagedRolloutSelectionModeHandler>());
        return services;
    }
}
