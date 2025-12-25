namespace ExperimentFramework.Models;

/// <summary>
/// Represents the configuration for a trial affecting a single service interface.
/// </summary>
/// <remarks>
/// <para>
/// A trial consists of:
/// <list type="bullet">
/// <item><description><b>Control</b>: The baseline implementation (always present)</description></item>
/// <item><description><b>Conditions</b>: Alternative implementations that may be selected based on rules</description></item>
/// <item><description><b>Selection Rule</b>: Determines WHEN conditions are activated</description></item>
/// <item><description><b>Behavior Rule</b>: Determines HOW the trial behaves (error handling, timeouts)</description></item>
/// </list>
/// </para>
/// <para>
/// Trials are contained within <see cref="Experiment"/>s, which can group multiple trials
/// across different service interfaces under a single named experiment.
/// </para>
/// </remarks>
public sealed class Trial
{
    /// <summary>
    /// Gets the service interface type being proxied.
    /// </summary>
    public required Type ServiceType { get; init; }

    /// <summary>
    /// Gets the key identifying the control (baseline) implementation.
    /// </summary>
    public required string ControlKey { get; init; }

    /// <summary>
    /// Gets the type of the control (baseline) implementation.
    /// </summary>
    public required Type ControlType { get; init; }

    /// <summary>
    /// Gets the map of condition key to implementation type.
    /// </summary>
    /// <remarks>
    /// This includes all conditions except the control. The control is stored separately
    /// in <see cref="ControlKey"/> and <see cref="ControlType"/>.
    /// </remarks>
    public required IReadOnlyDictionary<string, Type> Conditions { get; init; }

    /// <summary>
    /// Gets the selection rule that determines when conditions are activated.
    /// </summary>
    public required SelectionRule SelectionRule { get; init; }

    /// <summary>
    /// Gets the behavior rule that controls how the trial executes.
    /// </summary>
    public required BehaviorRule BehaviorRule { get; init; }

    /// <summary>
    /// Gets all implementations (control + conditions) as a single dictionary.
    /// </summary>
    /// <remarks>
    /// This provides a convenient way to access all implementations at once
    /// when you need the complete set of registered types.
    /// </remarks>
    public IReadOnlyDictionary<string, Type> AllImplementations
    {
        get
        {
            var all = new Dictionary<string, Type>(Conditions)
            {
                [ControlKey] = ControlType
            };
            return all;
        }
    }

    /// <summary>
    /// Returns a debug-friendly representation of the trial.
    /// </summary>
    public override string ToString()
        => $"Trial<{ServiceType.Name}> control='{ControlKey}' conditions=[{string.Join(",", Conditions.Keys)}]";
}
