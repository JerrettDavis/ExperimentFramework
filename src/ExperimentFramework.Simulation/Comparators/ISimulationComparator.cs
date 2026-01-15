namespace ExperimentFramework.Simulation.Comparators;

/// <summary>
/// Interface for comparing results between implementations in a simulation.
/// </summary>
/// <typeparam name="TResult">The result type to compare.</typeparam>
public interface ISimulationComparator<TResult>
{
    /// <summary>
    /// Compares two results and returns a list of differences.
    /// </summary>
    /// <param name="control">The control result.</param>
    /// <param name="condition">The condition result.</param>
    /// <param name="conditionName">The name of the condition being compared.</param>
    /// <returns>A list of difference descriptions, or an empty list if results are equivalent.</returns>
    IReadOnlyList<string> Compare(TResult? control, TResult? condition, string conditionName);
}
