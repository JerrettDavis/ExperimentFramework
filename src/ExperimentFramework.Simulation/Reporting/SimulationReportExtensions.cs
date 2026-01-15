using System.Text.Json;
using ExperimentFramework.Simulation.Models;

namespace ExperimentFramework.Simulation.Reporting;

/// <summary>
/// Extension methods for writing simulation reports.
/// </summary>
public static class SimulationReportExtensions
{
    /// <summary>
    /// Writes the simulation report as JSON to a file.
    /// </summary>
    /// <param name="report">The simulation report.</param>
    /// <param name="path">The file path to write to.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    public static void WriteJson(this SimulationReport report, string path, JsonSerializerOptions? options = null)
    {
        if (report == null) throw new ArgumentNullException(nameof(report));
        if (path == null) throw new ArgumentNullException(nameof(path));

        var jsonOptions = options ?? new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(report, jsonOptions);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Writes a text summary of the simulation report to a file.
    /// </summary>
    /// <param name="report">The simulation report.</param>
    /// <param name="path">The file path to write to.</param>
    public static void WriteSummary(this SimulationReport report, string path)
    {
        if (report == null) throw new ArgumentNullException(nameof(report));
        if (path == null) throw new ArgumentNullException(nameof(path));

        var lines = new List<string>
        {
            "===========================================",
            "Simulation Report",
            "===========================================",
            $"Timestamp: {report.Timestamp:yyyy-MM-dd HH:mm:ss UTC}",
            $"Service: {report.ServiceType}",
            $"Control: {report.ControlName}",
            $"Conditions: {string.Join(", ", report.ConditionNames)}",
            $"Status: {(report.Passed ? "PASSED" : "FAILED")}",
            "",
            "Summary:",
            report.Summary,
            "",
            "===========================================",
            "Scenario Results",
            "===========================================",
        };

        foreach (var result in report.ScenarioResults)
        {
            lines.AddRange(FormatScenarioResult(result));
        }

        File.WriteAllText(path, string.Join(Environment.NewLine, lines));
    }

    private static IEnumerable<string> FormatScenarioResult(IScenarioResult scenarioResult)
    {
        var lines = new List<string>
        {
            "",
            $"Scenario: {scenarioResult.ScenarioName}",
            $"  All Succeeded: {scenarioResult.AllSucceeded}",
            $"  Has Differences: {scenarioResult.HasDifferences}"
        };

        // Format control result
        lines.Add("  Control:");
        lines.AddRange(FormatImplementationResult(scenarioResult.Control, "    "));

        // Format condition results
        if (scenarioResult.Conditions.Any())
        {
            lines.Add("  Conditions:");
            foreach (var condition in scenarioResult.Conditions)
            {
                lines.AddRange(FormatImplementationResult(condition, "    "));
            }
        }

        // Format differences
        if (scenarioResult.Differences.Any())
        {
            lines.Add("  Differences:");
            foreach (var diff in scenarioResult.Differences)
            {
                lines.Add($"    - {diff}");
            }
        }

        return lines;
    }

    private static IEnumerable<string> FormatImplementationResult(IImplementationResult implResult, string indent)
    {
        var lines = new List<string>
        {
            $"{indent}{implResult.ImplementationName}:",
            $"{indent}  Success: {implResult.Success}",
            $"{indent}  Duration: {implResult.Duration.TotalMilliseconds:F2}ms"
        };

        if (implResult.Exception != null)
        {
            lines.Add($"{indent}  Exception: {implResult.Exception.GetType().Name}: {implResult.Exception.Message}");
        }

        return lines;
    }
}
