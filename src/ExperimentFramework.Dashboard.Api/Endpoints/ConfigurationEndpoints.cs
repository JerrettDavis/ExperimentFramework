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
            return Results.Ok("# No experiments configured");
        }

        // TODO: Implement YAML export from registry
        var yaml = "# ExperimentFramework Configuration\n# YAML export implementation pending\n";

        return Results.Ok(yaml);
    }
}
