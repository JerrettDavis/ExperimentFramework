namespace ExperimentFramework.Dashboard.UI.Models;

/// <summary>
/// Marker for Monaco Editor to display errors/warnings.
/// </summary>
public class EditorMarker
{
    public int Line { get; set; } = 1;
    public int Column { get; set; } = 1;
    public int EndLine { get; set; } = 1;
    public int EndColumn { get; set; } = 1;
    public string Message { get; set; } = "";
    public string Severity { get; set; } = "error"; // "error", "warning", "info"
}
