using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.ServiceRegistration;

/// <summary>
/// Generates human-readable and JSON reports from registration plans.
/// </summary>
public static class RegistrationPlanReport
{
    /// <summary>
    /// Generates a human-readable text report from a registration plan.
    /// </summary>
    /// <param name="plan">The registration plan to report on.</param>
    /// <returns>A formatted text report.</returns>
    public static string GenerateTextReport(RegistrationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var sb = new StringBuilder();
        sb.AppendLine("=== ExperimentFramework Registration Plan Report ===");
        sb.AppendLine();
        sb.AppendLine($"Plan ID: {plan.PlanId}");
        sb.AppendLine($"Created: {plan.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Validation Mode: {plan.ValidationMode}");
        sb.AppendLine($"Valid: {(plan.IsValid ? "YES" : "NO")}");
        sb.AppendLine();

        // Snapshot info
        sb.AppendLine("--- Service Graph Snapshot ---");
        sb.AppendLine($"Snapshot ID: {plan.Snapshot.SnapshotId}");
        sb.AppendLine($"Timestamp: {plan.Snapshot.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Descriptor Count: {plan.Snapshot.Descriptors.Count}");
        sb.AppendLine($"Fingerprint: {plan.Snapshot.Fingerprint}");
        sb.AppendLine();

        // Operations
        sb.AppendLine($"--- Patch Operations ({plan.Operations.Count}) ---");
        for (int i = 0; i < plan.Operations.Count; i++)
        {
            var op = plan.Operations[i];
            sb.AppendLine($"{i + 1}. {op.OperationType} - {op.ServiceType.Name}");
            sb.AppendLine($"   Operation ID: {op.OperationId}");
            sb.AppendLine($"   Service Type: {op.ServiceType.FullName}");
            sb.AppendLine($"   New Descriptors: {op.NewDescriptors.Count}");
            if (op.ExpectedMatchCount.HasValue)
            {
                sb.AppendLine($"   Expected Matches: {op.ExpectedMatchCount.Value}");
            }
            sb.AppendLine($"   Allow No Matches: {op.AllowNoMatches}");
            sb.AppendLine($"   Description: {op.Metadata.Description}");
            sb.AppendLine();
        }

        // Validation findings
        if (plan.Findings.Count > 0)
        {
            sb.AppendLine($"--- Validation Findings ({plan.Findings.Count}) ---");
            sb.AppendLine($"Errors: {plan.ErrorCount}, Warnings: {plan.WarningCount}");
            sb.AppendLine();

            foreach (var finding in plan.Findings.OrderByDescending(f => f.Severity))
            {
                var severitySymbol = finding.Severity switch
                {
                    ValidationSeverity.Error => "[ERROR]",
                    ValidationSeverity.Warning => "[WARN]",
                    ValidationSeverity.Info => "[INFO]",
                    _ => "[UNKNOWN]"
                };

                sb.AppendLine($"{severitySymbol} {finding.RuleName}");
                sb.AppendLine($"  Service: {finding.ServiceType.FullName}");
                sb.AppendLine($"  Issue: {finding.Description}");
                if (!string.IsNullOrEmpty(finding.RecommendedAction))
                {
                    sb.AppendLine($"  Action: {finding.RecommendedAction}");
                }
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("--- Validation Findings ---");
            sb.AppendLine("No issues found.");
            sb.AppendLine();
        }

        sb.AppendLine("=== End of Report ===");
        return sb.ToString();
    }

    /// <summary>
    /// Generates a JSON report from a registration plan.
    /// </summary>
    /// <param name="plan">The registration plan to report on.</param>
    /// <returns>A JSON string representation of the plan.</returns>
    public static string GenerateJsonReport(RegistrationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var report = new
        {
            planId = plan.PlanId,
            createdAt = plan.CreatedAt,
            validationMode = plan.ValidationMode.ToString(),
            isValid = plan.IsValid,
            snapshot = new
            {
                snapshotId = plan.Snapshot.SnapshotId,
                timestamp = plan.Snapshot.Timestamp,
                descriptorCount = plan.Snapshot.Descriptors.Count,
                fingerprint = plan.Snapshot.Fingerprint
            },
            operations = plan.Operations.Select(op => new
            {
                operationId = op.OperationId,
                operationType = op.OperationType.ToString(),
                serviceType = op.ServiceType.FullName,
                newDescriptorCount = op.NewDescriptors.Count,
                expectedMatchCount = op.ExpectedMatchCount,
                allowNoMatches = op.AllowNoMatches,
                metadata = new
                {
                    description = op.Metadata.Description,
                    properties = op.Metadata.Properties
                }
            }).ToArray(),
            findings = plan.Findings.Select(f => new
            {
                severity = f.Severity.ToString(),
                ruleName = f.RuleName,
                serviceType = f.ServiceType.FullName,
                description = f.Description,
                recommendedAction = f.RecommendedAction
            }).ToArray(),
            summary = new
            {
                operationCount = plan.Operations.Count,
                errorCount = plan.ErrorCount,
                warningCount = plan.WarningCount,
                hasErrors = plan.HasErrors,
                hasWarnings = plan.HasWarnings
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(report, options);
    }

    /// <summary>
    /// Generates a summary report for quick validation status.
    /// </summary>
    /// <param name="plan">The registration plan to summarize.</param>
    /// <returns>A brief summary string.</returns>
    public static string GenerateSummary(RegistrationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var status = plan.IsValid ? "VALID" : "INVALID";
        var emoji = plan.IsValid ? "✓" : "✗";

        return $"{emoji} Plan {plan.PlanId}: {status} | " +
               $"{plan.Operations.Count} operations | " +
               $"{plan.ErrorCount} errors | " +
               $"{plan.WarningCount} warnings";
    }
}
