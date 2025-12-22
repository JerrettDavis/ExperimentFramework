namespace ExperimentFramework.Models;
/// <summary>
/// Represents a configured experiment definition capable of producing an immutable registration.
/// </summary>
/// <remarks>
/// Definitions are typically produced by builders and then converted into <see cref="ExperimentRegistration"/>
/// instances that the runtime proxy uses to select and invoke trials.
/// </remarks>
internal interface IExperimentDefinition
{
    /// <summary>
    /// Gets the service interface type being experimented on (the proxy type exposed to callers).
    /// </summary>
    Type ServiceType { get; }

    /// <summary>
    /// Creates an immutable registration for the definition.
    /// </summary>
    /// <param name="serviceProvider">A service provider that can be used to read configuration if needed.</param>
    /// <returns>An experiment registration.</returns>
    ExperimentRegistration CreateRegistration(IServiceProvider serviceProvider);
}
