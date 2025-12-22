using System.Collections.Concurrent;
using ExperimentFramework.Models;

namespace ExperimentFramework;

/// <summary>
/// Provides fast lookup of experiment registrations by service type.
/// </summary>
/// <remarks>
/// The registry is intended to be a stable map for the lifetime of the container.
/// </remarks>
internal sealed class ExperimentRegistry
{
    private readonly ConcurrentDictionary<Type, ExperimentRegistration> _registrations = new();

    /// <summary>
    /// Initializes the registry by materializing registrations from definitions.
    /// </summary>
    /// <param name="defs">Experiment definitions.</param>
    /// <param name="rootProvider">Root service provider used when creating registrations.</param>
    public ExperimentRegistry(IEnumerable<IExperimentDefinition> defs, IServiceProvider rootProvider)
    {
        foreach (var def in defs)
            _registrations.TryAdd(def.ServiceType, def.CreateRegistration(rootProvider));
    }

    /// <summary>
    /// Attempts to retrieve the registration for a service type.
    /// </summary>
    /// <param name="serviceType">The proxied service interface type.</param>
    /// <param name="registration">The registration if found.</param>
    /// <returns><see langword="true"/> if the registration exists; otherwise <see langword="false"/>.</returns>
    public bool TryGet(Type serviceType, out ExperimentRegistration registration)
        => _registrations.TryGetValue(serviceType, out registration!);
}


