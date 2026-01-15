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

    private static IEnumerable<string> FormatScenarioResult(object scenarioResult)
    {
        // Use reflection to extract scenario result details
        var type = scenarioResult.GetType();
        
        var scenarioName = type.GetProperty("ScenarioName")?.GetValue(scenarioResult)?.ToString() ?? "Unknown";
        var allSucceeded = type.GetProperty("AllSucceeded")?.GetValue(scenarioResult) as bool? ?? false;
        var hasDifferences = type.GetProperty("HasDifferences")?.GetValue(scenarioResult) as bool? ?? false;

        var lines = new List<string>
        {
            "",
            $"Scenario: {scenarioName}",
            $"  All Succeeded: {allSucceeded}",
            $"  Has Differences: {hasDifferences}"
        };

        // Get control result
        var control = type.GetProperty("Control")?.GetValue(scenarioResult);
        if (control != null)
        {
            lines.Add("  Control:");
            lines.AddRange(FormatImplementationResult(control, "    "));
        }

        // Get condition results
        var conditions = type.GetProperty("Conditions")?.GetValue(scenarioResult);
        if (conditions is System.Collections.IEnumerable enumerable)
        {
            var conditionList = enumerable.Cast<object>().ToList();
            if (conditionList.Any())
            {
                lines.Add("  Conditions:");
                foreach (var condition in conditionList)
                {
                    lines.AddRange(FormatImplementationResult(condition, "    "));
                }
            }
        }

        // Get differences
        var differences = type.GetProperty("Differences")?.GetValue(scenarioResult);
        if (differences is System.Collections.IEnumerable diffEnumerable)
        {
            var diffList = diffEnumerable.Cast<object>().Select(d => d.ToString()).ToList();
            if (diffList.Any())
            {
                lines.Add("  Differences:");
                foreach (var diff in diffList)
                {
                    lines.Add($"    - {diff}");
                }
            }
        }

        return lines;
    }

    private static IEnumerable<string> FormatImplementationResult(object implResult, string indent)
    {
        var type = implResult.GetType();
        var name = type.GetProperty("ImplementationName")?.GetValue(implResult)?.ToString() ?? "Unknown";
        var success = type.GetProperty("Success")?.GetValue(implResult) as bool? ?? false;
        var duration = type.GetProperty("Duration")?.GetValue(implResult) as TimeSpan?;
        var exception = type.GetProperty("Exception")?.GetValue(implResult) as Exception;

        var lines = new List<string>
        {
            $"{indent}{name}:",
            $"{indent}  Success: {success}",
            $"{indent}  Duration: {duration?.TotalMilliseconds:F2}ms"
        };

        if (exception != null)
        {
            lines.Add($"{indent}  Exception: {exception.GetType().Name}: {exception.Message}");
        }

        return lines;
    }
}
