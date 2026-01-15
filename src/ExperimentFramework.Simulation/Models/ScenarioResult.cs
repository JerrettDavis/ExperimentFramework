namespace ExperimentFramework.Simulation.Models;

/// <summary>
/// Represents the execution result for a single implementation in a scenario.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
public sealed class ImplementationResult<TResult> : IImplementationResult
{
    /// <summary>
    /// Gets the name of the implementation.
    /// </summary>
    public string ImplementationName { get; }

    /// <summary>
    /// Gets the result value if execution was successful.
    /// </summary>
    public TResult? Result { get; }

    /// <summary>
    /// Gets the exception if execution failed.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the execution duration.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets whether the execution was successful.
    /// </summary>
    public bool Success => Exception == null;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImplementationResult{TResult}"/> class.
    /// </summary>
    public ImplementationResult(string implementationName, TResult? result, Exception? exception, TimeSpan duration)
    {
        ImplementationName = implementationName ?? throw new ArgumentNullException(nameof(implementationName));
        Result = result;
        Exception = exception;
        Duration = duration;
    }

    /// <inheritdoc/>
    public string? ResultAsString() => Result?.ToString();
}

/// <summary>
/// Represents the result of executing a single scenario across multiple implementations.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
public sealed class ScenarioResult<TResult> : IScenarioResult
{
    /// <summary>
    /// Gets the scenario name.
    /// </summary>
    public string ScenarioName { get; }

    /// <summary>
    /// Gets the control result.
    /// </summary>
    public ImplementationResult<TResult> Control { get; }

    /// <summary>
    /// Gets the condition results.
    /// </summary>
    public IReadOnlyList<ImplementationResult<TResult>> Conditions { get; }

    /// <summary>
    /// Gets the selected implementation name.
    /// </summary>
    public string SelectedImplementation { get; }

    /// <summary>
    /// Gets the differences detected between implementations.
    /// </summary>
    public IReadOnlyList<string> Differences { get; }

    /// <summary>
    /// Gets whether all implementations succeeded.
    /// </summary>
    public bool AllSucceeded => Control.Success && Conditions.All(c => c.Success);

    /// <summary>
    /// Gets whether any differences were detected.
    /// </summary>
    public bool HasDifferences => Differences.Count > 0;

    // Explicit interface implementations for non-generic access
    IImplementationResult IScenarioResult.Control => Control;
    IReadOnlyList<IImplementationResult> IScenarioResult.Conditions => Conditions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioResult{TResult}"/> class.
    /// </summary>
    public ScenarioResult(
        string scenarioName,
        ImplementationResult<TResult> control,
        IReadOnlyList<ImplementationResult<TResult>> conditions,
        string selectedImplementation,
        IReadOnlyList<string> differences)
    {
        ScenarioName = scenarioName ?? throw new ArgumentNullException(nameof(scenarioName));
        Control = control ?? throw new ArgumentNullException(nameof(control));
        Conditions = conditions ?? throw new ArgumentNullException(nameof(conditions));
        SelectedImplementation = selectedImplementation ?? throw new ArgumentNullException(nameof(selectedImplementation));
        Differences = differences ?? throw new ArgumentNullException(nameof(differences));
    }
}
