namespace ExperimentFramework.Simulation.Models;

/// <summary>
/// Represents a test scenario with inputs for simulation execution.
/// </summary>
/// <typeparam name="TService">The service interface type being tested.</typeparam>
/// <typeparam name="TResult">The result type returned by the scenario.</typeparam>
public sealed class Scenario<TService, TResult>
{
    /// <summary>
    /// Gets the name of the scenario.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the function to execute against the service.
    /// </summary>
    public Func<TService, ValueTask<TResult>> Execute { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Scenario{TService, TResult}"/> class.
    /// </summary>
    /// <param name="name">The scenario name.</param>
    /// <param name="execute">The function to execute.</param>
    public Scenario(string name, Func<TService, ValueTask<TResult>> execute)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }
}
