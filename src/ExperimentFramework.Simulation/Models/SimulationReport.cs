namespace ExperimentFramework.Simulation.Models;

/// <summary>
/// Represents the overall simulation report containing all scenario results.
/// </summary>
public sealed class SimulationReport
{
    /// <summary>
    /// Gets the timestamp when the simulation was executed.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the service interface that was simulated.
    /// </summary>
    public string ServiceType { get; }

    /// <summary>
    /// Gets the control implementation name.
    /// </summary>
    public string ControlName { get; }

    /// <summary>
    /// Gets the condition implementation names.
    /// </summary>
    public IReadOnlyList<string> ConditionNames { get; }

    /// <summary>
    /// Gets the scenario results (stored as object to handle different TResult types).
    /// </summary>
    public IReadOnlyList<object> ScenarioResults { get; }

    /// <summary>
    /// Gets the overall pass/fail status.
    /// </summary>
    public bool Passed { get; }

    /// <summary>
    /// Gets the summary message.
    /// </summary>
    public string Summary { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationReport"/> class.
    /// </summary>
    public SimulationReport(
        string serviceType,
        string controlName,
        IReadOnlyList<string> conditionNames,
        IReadOnlyList<object> scenarioResults,
        bool passed,
        string summary)
    {
        Timestamp = DateTimeOffset.UtcNow;
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        ControlName = controlName ?? throw new ArgumentNullException(nameof(controlName));
        ConditionNames = conditionNames ?? throw new ArgumentNullException(nameof(conditionNames));
        ScenarioResults = scenarioResults ?? throw new ArgumentNullException(nameof(scenarioResults));
        Passed = passed;
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
    }
}
