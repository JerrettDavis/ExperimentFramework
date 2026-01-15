using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Simulation.Builders;

/// <summary>
/// Entry point for creating simulation runners.
/// </summary>
public static class SimulationRunner
{
    /// <summary>
    /// Creates a new simulation runner builder.
    /// </summary>
    /// <param name="serviceProvider">The service provider containing the service implementations.</param>
    /// <returns>A builder for configuring the simulation.</returns>
    public static SimulationRunnerBuilder Create(IServiceProvider serviceProvider)
    {
        return new SimulationRunnerBuilder(serviceProvider);
    }
}

/// <summary>
/// Builder for selecting the service type to simulate.
/// </summary>
public sealed class SimulationRunnerBuilder
{
    private readonly IServiceProvider _serviceProvider;

    internal SimulationRunnerBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Specifies the service interface to simulate.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <returns>A builder for configuring result type and scenarios.</returns>
    public SimulationServiceBuilder<TService> For<TService>() where TService : class
    {
        return new SimulationServiceBuilder<TService>(_serviceProvider);
    }
}

/// <summary>
/// Builder for configuring the service and result type.
/// </summary>
/// <typeparam name="TService">The service interface type.</typeparam>
public sealed class SimulationServiceBuilder<TService> where TService : notnull
{
    private readonly IServiceProvider _serviceProvider;

    internal SimulationServiceBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Creates a simulation runner with the specified result type.
    /// </summary>
    /// <typeparam name="TResult">The result type returned by the service methods.</typeparam>
    /// <returns>A simulation runner for configuring and executing scenarios.</returns>
    public SimulationRunner<TService, TResult> WithResultType<TResult>()
    {
        return new SimulationRunner<TService, TResult>(_serviceProvider);
    }

    /// <summary>
    /// Creates a simulation runner (infers result type from scenarios).
    /// Note: When using this method, you'll need to provide an explicit result type when calling methods.
    /// </summary>
    /// <typeparam name="TResult">The result type returned by the service methods.</typeparam>
    /// <returns>A simulation runner for configuring and executing scenarios.</returns>
    public SimulationRunner<TService, TResult> AsRunnerFor<TResult>()
    {
        return new SimulationRunner<TService, TResult>(_serviceProvider);
    }
}
