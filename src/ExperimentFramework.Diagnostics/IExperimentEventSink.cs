namespace ExperimentFramework.Diagnostics;

/// <summary>
/// Defines a sink for capturing experiment events.
/// </summary>
/// <remarks>
/// Implementations should be thread-safe and minimize allocations during event capture.
/// The <c>in</c> parameter modifier ensures events are passed by reference for efficiency.
/// </remarks>
public interface IExperimentEventSink
{
    /// <summary>
    /// Captures an experiment event.
    /// </summary>
    /// <param name="e">The event to capture.</param>
    /// <remarks>
    /// This method is called synchronously during experiment execution.
    /// Implementations should avoid blocking operations and complete quickly.
    /// </remarks>
    void OnEvent(in ExperimentEvent e);
}
