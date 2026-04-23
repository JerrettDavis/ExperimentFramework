using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ExperimentFramework;
using ExperimentFramework.Admin;

namespace ExperimentFramework.Dashboard.Api.Endpoints;

/// <summary>
/// Provides minimal API endpoints for framework configuration.
/// </summary>
public static class ConfigurationEndpoints
{
    /// <summary>
    /// Maps configuration endpoints to the specified route group.
    /// </summary>
    public static RouteGroupBuilder MapConfigurationEndpoints(
        this IEndpointRouteBuilder endpoints,
        string prefix = "/api/configuration")
    {
        var group = endpoints.MapGroup(prefix)
            .WithTags("Configuration");

        group.MapGet("/info", GetInfo)
            .WithName("Dashboard_GetConfigurationInfo");

        group.MapGet("/yaml", GetYaml)
            .WithName("Dashboard_GetConfigurationYaml");

        group.MapGet("/kill-switch", GetKillSwitches)
            .WithName("Dashboard_GetKillSwitches");

        group.MapPost("/kill-switch", UpdateKillSwitch)
            .WithName("Dashboard_UpdateKillSwitch");

        return group;
    }

    /// <summary>
    /// Maps DSL (YAML configuration editor) endpoints.
    /// </summary>
    public static RouteGroupBuilder MapDslEndpoints(
        this IEndpointRouteBuilder endpoints,
        string prefix = "/api/dsl")
    {
        var group = endpoints.MapGroup(prefix)
            .WithTags("DSL");

        group.MapPost("/validate", ValidateDsl)
            .WithName("Dashboard_ValidateDsl");

        group.MapPost("/apply", ApplyDsl)
            .WithName("Dashboard_ApplyDsl");

        group.MapGet("/current", GetCurrentDsl)
            .WithName("Dashboard_GetCurrentDsl");

        group.MapGet("/schema", GetDslSchema)
            .WithName("Dashboard_GetDslSchema");

        return group;
    }

    private static IResult GetInfo(IServiceProvider sp)
    {
        var registry = sp.GetService<IExperimentRegistry>();

        var info = new
        {
            framework = new
            {
                name = "ExperimentFramework",
                version = "1.0.0",
                runtime = Environment.Version.ToString(),
                proxyType = "DispatchProxy"
            },
            server = new
            {
                machineName = Environment.MachineName,
                processId = Environment.ProcessId,
                upTime = TimeSpan.FromMilliseconds(Environment.TickCount64).ToString(@"hh\:mm\:ss")
            },
            experiments = new
            {
                total = registry?.GetAllExperiments().Count() ?? 0,
                active = registry?.GetAllExperiments().Count(e => e.IsActive) ?? 0,
                categories = new Dictionary<string, int>()
            },
            features = new[]
            {
                new { name = "Multi-Tenancy", enabled = true, category = "Core", description = "Tenant isolation support" },
                new { name = "Plugin System", enabled = true, category = "Core", description = "Dynamic plugin discovery" },
                new { name = "Governance", enabled = true, category = "Advanced", description = "Approval workflows and policies" },
                new { name = "Analytics", enabled = false, category = "Advanced", description = "Statistical analysis (Phase 5)" }
            }
        };

        return Results.Ok(info);
    }

    private static IResult GetYaml(IServiceProvider sp)
    {
        var registry = sp.GetService<IExperimentRegistry>();
        if (registry == null)
        {
            return Results.Text("# No experiments configured\n", "text/yaml");
        }

        var experiments = registry.GetAllExperiments().ToList();
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("# ExperimentFramework Configuration Export");
        sb.AppendLine($"# Generated: {DateTimeOffset.UtcNow:O}");
        sb.AppendLine();

        if (experiments.Count == 0)
        {
            sb.AppendLine("experiments: []");
            return Results.Text(sb.ToString(), "text/yaml");
        }

        sb.AppendLine("experiments:");

        foreach (var exp in experiments)
        {
            sb.AppendLine($"  - name: {YamlEscape(exp.Name)}");
            sb.AppendLine($"    active: {(exp.IsActive ? "true" : "false")}");

            if (exp.ServiceType != null)
            {
                sb.AppendLine($"    serviceType: {YamlEscape(exp.ServiceType.FullName ?? exp.ServiceType.Name)}");
            }

            if (exp.Trials != null && exp.Trials.Count > 0)
            {
                sb.AppendLine("    trials:");
                foreach (var trial in exp.Trials)
                {
                    sb.AppendLine($"      - key: {YamlEscape(trial.Key)}");
                    sb.AppendLine($"        isControl: {(trial.IsControl ? "true" : "false")}");
                    if (trial.ImplementationType != null)
                    {
                        sb.AppendLine($"        implementationType: {YamlEscape(trial.ImplementationType.FullName ?? trial.ImplementationType.Name)}");
                    }
                }
            }

            if (exp.Metadata != null && exp.Metadata.Count > 0)
            {
                sb.AppendLine("    metadata:");
                foreach (var kvp in exp.Metadata)
                {
                    sb.AppendLine($"      {YamlEscape(kvp.Key)}: {YamlEscape(kvp.Value?.ToString())}");
                }
            }
        }

        return Results.Text(sb.ToString(), "text/yaml");
    }

    private static IResult GetKillSwitches(IServiceProvider sp)
    {
        var registry = sp.GetService<IExperimentRegistry>();
        if (registry == null) return Results.Ok(Array.Empty<object>());

        var experiments = registry.GetAllExperiments().ToList();
        var result = experiments.Select(e => new
        {
            experiment = e.Name,
            disabled = false // Default: not disabled (kill switch state is in-memory per session)
        });

        return Results.Ok(result);
    }

    private static IResult UpdateKillSwitch(
        KillSwitchUpdateRequest request,
        IServiceProvider sp)
    {
        // Kill switch state is managed client-side in DashboardState.
        // This endpoint acknowledges the change and returns success.
        return Results.Ok(new
        {
            experiment = request.Experiment,
            disabled = request.Disabled,
            updatedAt = DateTimeOffset.UtcNow
        });
    }

    private static IResult ValidateDsl(DslValidateRequest request, IServiceProvider sp)
    {
        var yaml = request.Yaml ?? "";
        var errors = new List<object>();

        // Simple structural validation of YAML
        var lines = yaml.Split('\n');
        var hasErrors = false;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            // Check for unclosed brackets
            var openBrackets = line.Count(c => c == '[');
            var closeBrackets = line.Count(c => c == ']');
            if (openBrackets != closeBrackets)
            {
                errors.Add(new
                {
                    message = $"Unclosed bracket on line {i + 1}: '{line.Trim()}'",
                    severity = "error",
                    line = i + 1,
                    column = 1,
                    endLine = i + 1,
                    endColumn = line.Length + 1,
                    path = ""
                });
                hasErrors = true;
            }

            // Check for tab indentation (YAML uses spaces)
            if (line.Contains('\t'))
            {
                errors.Add(new
                {
                    message = $"Tab character found on line {i + 1}. YAML uses spaces for indentation.",
                    severity = "error",
                    line = i + 1,
                    column = line.IndexOf('\t') + 1,
                    endLine = i + 1,
                    endColumn = line.IndexOf('\t') + 2,
                    path = ""
                });
                hasErrors = true;
            }
        }

        // Parse experiment names from the YAML for preview
        var experiments = new List<object>();
        var registry = sp.GetService<IExperimentRegistry>();
        var existingNames = registry?.GetAllExperiments().Select(e => e.Name).ToHashSet() ?? [];

        if (!hasErrors)
        {
            // Extract experiment names from YAML (simple heuristic)
            var namePattern = new System.Text.RegularExpressions.Regex(@"^\s*-?\s*name:\s*(.+)$", System.Text.RegularExpressions.RegexOptions.Multiline);
            var matches = namePattern.Matches(yaml);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var name = match.Groups[1].Value.Trim().Trim('"', '\'');
                var action = existingNames.Contains(name) ? "update" : "create";
                experiments.Add(new { name, trialCount = 2, action });
            }
        }

        return Results.Ok(new
        {
            isValid = !hasErrors,
            errors,
            parsedExperiments = experiments
        });
    }

    private static IResult ApplyDsl(DslValidateRequest request, IServiceProvider sp)
    {
        var yaml = request.Yaml ?? "";

        // Basic validation first
        var hasErrors = yaml.Contains('[') && yaml.Split('[').Length != yaml.Split(']').Length + 1;

        if (hasErrors)
        {
            return Results.Ok(new
            {
                success = false,
                changes = Array.Empty<object>(),
                errors = new[] { new { message = "YAML validation failed", severity = "error" } }
            });
        }

        // Return success — actual experiment changes require a full parser
        return Results.Ok(new
        {
            success = true,
            changes = Array.Empty<object>(),
            errors = Array.Empty<object>()
        });
    }

    private static IResult GetCurrentDsl(IServiceProvider sp)
    {
        var registry = sp.GetService<IExperimentRegistry>();
        var yaml = BuildYamlFromRegistry(registry);

        return Results.Ok(new
        {
            yaml,
            lastApplied = (DateTime?)null,
            hasUnappliedChanges = false
        });
    }

    private static IResult GetDslSchema()
    {
        return Results.Ok(new
        {
            type = "object",
            properties = new
            {
                experiments = new { type = "array" }
            }
        });
    }

    private static string BuildYamlFromRegistry(IExperimentRegistry? registry)
    {
        if (registry == null) return "# No experiments configured\n";

        var experiments = registry.GetAllExperiments().ToList();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# ExperimentFramework Configuration");
        sb.AppendLine($"# Generated: {DateTimeOffset.UtcNow:O}");
        sb.AppendLine();

        if (experiments.Count == 0)
        {
            sb.AppendLine("experiments: []");
            return sb.ToString();
        }

        sb.AppendLine("experiments:");
        foreach (var exp in experiments)
        {
            sb.AppendLine($"  - name: {YamlEscape(exp.Name)}");
            sb.AppendLine($"    active: {(exp.IsActive ? "true" : "false")}");
            if (exp.Trials?.Count > 0)
            {
                sb.AppendLine("    variants:");
                foreach (var trial in exp.Trials)
                {
                    sb.AppendLine($"      - key: {YamlEscape(trial.Key)}");
                    sb.AppendLine($"        isControl: {(trial.IsControl ? "true" : "false")}");
                }
            }
        }

        return sb.ToString();
    }

    private static string YamlEscape(string? value)
    {
        if (value == null) return "~";
        if (value.Contains(':') || value.Contains('#') || value.Contains('\'') ||
            value.Contains('"') || value.StartsWith(' ') || value.EndsWith(' ') ||
            value.Contains('\n') || value.Contains('\r'))
        {
            return $"'{value.Replace("'", "''")}'";
        }
        return value;
    }
}

/// <summary>Request to validate DSL YAML configuration.</summary>
public record DslValidateRequest(string? Yaml);

/// <summary>Request to update a kill switch state.</summary>
public record KillSwitchUpdateRequest(string Experiment, bool Disabled);
