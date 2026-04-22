using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ExperimentFramework.Dashboard.Abstractions;

namespace ExperimentFramework.Dashboard.Api.Endpoints;

/// <summary>
/// Provides minimal API endpoints for plugin management.
/// </summary>
public static class PluginEndpoints
{
    /// <summary>
    /// Maps plugin endpoints to the specified route group.
    /// </summary>
    public static RouteGroupBuilder MapPluginEndpoints(
        this IEndpointRouteBuilder endpoints,
        string prefix = "/api/plugins")
    {
        var group = endpoints.MapGroup(prefix)
            .WithTags("Plugins");

        group.MapGet("/", GetPlugins)
            .WithName("Dashboard_GetPlugins");

        group.MapPost("/discover", DiscoverPlugins)
            .WithName("Dashboard_DiscoverPlugins");

        group.MapPost("/reload", ReloadPlugins)
            .WithName("Dashboard_ReloadPlugins");

        return group;
    }

    private static async Task<IResult> GetPlugins(IServiceProvider sp, CancellationToken ct)
    {
        var pluginService = sp.GetService<IPluginManagementService>();
        if (pluginService == null)
        {
            return Results.StatusCode(501);
        }

        var plugins = await pluginService.GetLoadedPluginsAsync(ct);
        return Results.Ok(new { plugins });
    }

    private static async Task<IResult> DiscoverPlugins(IServiceProvider sp, CancellationToken ct)
    {
        var pluginService = sp.GetService<IPluginManagementService>();
        if (pluginService == null)
        {
            return Results.StatusCode(501);
        }

        var discovered = await pluginService.DiscoverPluginsAsync(ct);
        return Results.Ok(new
        {
            discoveredCount = discovered.Count,
            plugins = discovered
        });
    }

    private static async Task<IResult> ReloadPlugins(IServiceProvider sp, CancellationToken ct)
    {
        var pluginService = sp.GetService<IPluginManagementService>();
        if (pluginService == null)
        {
            return Results.StatusCode(501);
        }

        var reloaded = await pluginService.ReloadAllPluginsAsync(ct);
        return Results.Ok(new
        {
            reloadedCount = reloaded.Count,
            plugins = reloaded
        });
    }
}
