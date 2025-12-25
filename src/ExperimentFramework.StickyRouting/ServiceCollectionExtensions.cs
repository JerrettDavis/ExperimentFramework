using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.StickyRouting;

/// <summary>
/// Extension methods for registering sticky routing support.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds sticky routing selection mode support to the experiment framework.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the <see cref="StickyRoutingProvider"/> which enables
    /// the <c>.UsingStickyRouting()</c> selection mode.
    /// </para>
    /// <para>
    /// You must also register an <see cref="IExperimentIdentityProvider"/> implementation:
    /// <code>
    /// services.AddScoped&lt;IExperimentIdentityProvider, MyIdentityProvider&gt;();
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddExperimentStickyRouting();
    /// services.AddScoped&lt;IExperimentIdentityProvider, UserIdProvider&gt;();
    /// services.AddExperimentFramework(builder);
    /// </code>
    /// </example>
    public static IServiceCollection AddExperimentStickyRouting(this IServiceCollection services)
        => services.AddSelectionModeProvider<StickyRoutingProvider>();
}
