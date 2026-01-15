namespace ExperimentFramework.Simulation.Models;

/// <summary>
/// Non-generic interface for scenario results to enable type-safe reporting without reflection.
/// </summary>
public interface IScenarioResult
{
    /// <summary>
    /// Gets the scenario name.
    /// </summary>
    string ScenarioName { get; }

    /// <summary>
    /// Gets whether all implementations succeeded.
    /// </summary>
    bool AllSucceeded { get; }

    /// <summary>
    /// Gets whether any differences were detected.
    /// </summary>
    bool HasDifferences { get; }

    /// <summary>
    /// Gets the selected implementation name.
    /// </summary>
    string SelectedImplementation { get; }

    /// <summary>
    /// Gets the differences detected between implementations.
    /// </summary>
    IReadOnlyList<string> Differences { get; }

    /// <summary>
    /// Gets the control implementation result.
    /// </summary>
    IImplementationResult Control { get; }

    /// <summary>
    /// Gets the condition implementation results.
    /// </summary>
    IReadOnlyList<IImplementationResult> Conditions { get; }
}

/// <summary>
/// Non-generic interface for implementation results.
/// </summary>
public interface IImplementationResult
{
    /// <summary>
    /// Gets the name of the implementation.
    /// </summary>
    string ImplementationName { get; }

    /// <summary>
    /// Gets whether the execution was successful.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the execution duration.
    /// </summary>
    TimeSpan Duration { get; }

    /// <summary>
    /// Gets the exception if execution failed.
    /// </summary>
    Exception? Exception { get; }

    /// <summary>
    /// Gets the result value as a formatted string.
    /// </summary>
    string? ResultAsString();
}
