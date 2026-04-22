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
