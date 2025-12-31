using ExperimentFramework.Decorators;
using ExperimentFramework.ServiceRegistration;

namespace ExperimentFramework.Models;

/// <summary>
/// Immutable configuration snapshot produced by <see cref="ExperimentFrameworkBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ExperimentFrameworkConfiguration"/> represents the fully materialized configuration
/// for the experiment framework at application startup.
/// </para>
/// <para>
/// It is intentionally simple and immutable, acting as a transport object between:
/// </para>
/// <list type="bullet">
/// <item><description>The declarative builder API.</description></item>
/// <item><description>The dependency injection integration layer.</description></item>
/// <item><description>The runtime proxy and decorator pipeline.</description></item>
/// </list>
/// <para>
/// Once created, this configuration should be treated as read-only for the lifetime of the application.
/// </para>
/// </remarks>
/// <param name="DecoratorFactories">
/// The ordered set of decorator factories used to construct the global decorator pipeline.
/// Decorators are applied outer-to-inner in the order defined here.
/// </param>
/// <param name="Definitions">
/// The set of experiment definitions describing which services participate in experiments
/// and how their trials are selected.
/// </param>
/// <param name="UseRuntimeProxies">
/// If true, uses DispatchProxy-based runtime proxies instead of source-generated compile-time proxies.
/// Defaults to false (source generators).
/// </param>
/// <param name="RegistrationSafetyEnabled">
/// If true, uses the registration plan system to validate and safely mutate service registrations.
/// Defaults to true for enterprise safety.
/// </param>
/// <param name="RegistrationValidationMode">
/// The validation mode to use when registration safety is enabled.
/// Defaults to Strict mode.
/// </param>
/// <param name="DefaultMultiRegistrationBehavior">
/// The default behavior for handling multi-registration scenarios.
/// Defaults to Replace.
/// </param>
/// <param name="EmitRegistrationReport">
/// If true, generates and logs a registration plan report.
/// Defaults to false.
/// </param>
internal sealed record ExperimentFrameworkConfiguration(
    IExperimentDecoratorFactory[] DecoratorFactories,
    IExperimentDefinition[] Definitions,
    bool UseRuntimeProxies = false,
    bool RegistrationSafetyEnabled = true,
    ValidationMode RegistrationValidationMode = ValidationMode.Strict,
    MultiRegistrationBehavior DefaultMultiRegistrationBehavior = MultiRegistrationBehavior.Replace,
    bool EmitRegistrationReport = false
);