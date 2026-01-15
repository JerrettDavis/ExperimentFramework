namespace ExperimentFramework.Simulation.Models;

/// <summary>
/// Options for configuring shadow mode execution behavior.
/// </summary>
public sealed class ShadowModeOptions
{
    /// <summary>
    /// Gets or sets which result to return to the caller.
    /// </summary>
    public ResultReturnMode ReturnMode { get; set; } = ResultReturnMode.Control;

    /// <summary>
    /// Gets or sets whether to fail the simulation if differences are detected.
    /// </summary>
    public bool FailOnDifference { get; set; } = false;
}

/// <summary>
/// Specifies which result should be returned from shadow execution.
/// </summary>
public enum ResultReturnMode
{
    /// <summary>
    /// Return the control result.
    /// </summary>
    Control,

    /// <summary>
    /// Return the selected implementation result.
    /// </summary>
    Selected
}
