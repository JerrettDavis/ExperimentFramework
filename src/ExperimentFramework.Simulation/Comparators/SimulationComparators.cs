using System.Text.Json;

namespace ExperimentFramework.Simulation.Comparators;

/// <summary>
/// Factory for creating common simulation comparators.
/// </summary>
public static class SimulationComparators
{
    /// <summary>
    /// Creates an equality comparator.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <returns>An equality comparator instance.</returns>
    public static ISimulationComparator<TResult> Equality<TResult>()
    {
        return new EqualityComparator<TResult>();
    }

    /// <summary>
    /// Creates a JSON structural comparator.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>A JSON comparator instance.</returns>
    public static ISimulationComparator<TResult> Json<TResult>(JsonSerializerOptions? options = null)
    {
        return new JsonComparator<TResult>(options);
    }
}
