using System.Text.Json.Serialization;
using ExperimentFramework.Plugins.Abstractions;

namespace ExperimentFramework.Plugins.Manifest;

/// <summary>
/// Concrete implementation of <see cref="IPluginManifest"/>.
/// </summary>
public sealed record PluginManifest : IPluginManifest
{
    /// <inheritdoc />
    [JsonPropertyName("manifestVersion")]
    public required string ManifestVersion { get; init; }

    /// <inheritdoc />
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <inheritdoc />
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <inheritdoc />
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    /// <inheritdoc />
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <inheritdoc />
    [JsonPropertyName("isolation")]
    public PluginIsolationConfig Isolation { get; init; } = new();

    /// <inheritdoc />
    [JsonPropertyName("services")]
    public IReadOnlyList<PluginServiceRegistration> Services { get; init; } = [];

    /// <inheritdoc />
    [JsonPropertyName("lifecycle")]
    public PluginLifecycleConfig Lifecycle { get; init; } = new();

    /// <summary>
    /// Creates a default manifest for plugins without an explicit manifest file.
    /// </summary>
    /// <param name="pluginId">The plugin identifier (typically derived from assembly name).</param>
    /// <param name="version">The version (typically from assembly version).</param>
    /// <returns>A default manifest.</returns>
    public static PluginManifest CreateDefault(string pluginId, string version = "1.0.0") => new()
    {
        ManifestVersion = "1.0",
        Id = pluginId,
        Name = pluginId,
        Version = version
    };
}

/// <summary>
/// JSON wrapper for the plugin manifest file format.
/// </summary>
internal sealed record PluginManifestJson
{
    [JsonPropertyName("manifestVersion")]
    public string ManifestVersion { get; init; } = "1.0";

    [JsonPropertyName("plugin")]
    public PluginInfoJson? Plugin { get; init; }

    [JsonPropertyName("isolation")]
    public IsolationConfigJson? Isolation { get; init; }

    [JsonPropertyName("services")]
    public List<ServiceRegistrationJson>? Services { get; init; }

    [JsonPropertyName("lifecycle")]
    public LifecycleConfigJson? Lifecycle { get; init; }

    public PluginManifest ToManifest()
    {
        var isolation = new PluginIsolationConfig
        {
            Mode = Isolation?.Mode switch
            {
                "full" => PluginIsolationMode.Full,
                "none" => PluginIsolationMode.None,
                _ => PluginIsolationMode.Shared
            },
            SharedAssemblies = Isolation?.SharedAssemblies ?? []
        };

        var services = (Services ?? [])
            .Select(s => new PluginServiceRegistration
            {
                Interface = s.Interface ?? throw new InvalidOperationException("Service interface is required"),
                Implementations = (s.Implementations ?? [])
                    .Select(i => new PluginImplementation
                    {
                        Type = i.Type ?? throw new InvalidOperationException("Implementation type is required"),
                        Alias = i.Alias
                    })
                    .ToList()
            })
            .ToList();

        var lifecycle = new PluginLifecycleConfig
        {
            SupportsHotReload = Lifecycle?.SupportsHotReload ?? true,
            RequiresRestartOnUnload = Lifecycle?.RequiresRestartOnUnload ?? false
        };

        return new PluginManifest
        {
            ManifestVersion = ManifestVersion,
            Id = Plugin?.Id ?? throw new InvalidOperationException("Plugin id is required"),
            Name = Plugin?.Name ?? Plugin?.Id ?? "Unknown",
            Version = Plugin?.Version ?? "1.0.0",
            Description = Plugin?.Description,
            Isolation = isolation,
            Services = services,
            Lifecycle = lifecycle
        };
    }
}

internal sealed record PluginInfoJson
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

internal sealed record IsolationConfigJson
{
    [JsonPropertyName("mode")]
    public string? Mode { get; init; }

    [JsonPropertyName("sharedAssemblies")]
    public List<string>? SharedAssemblies { get; init; }
}

internal sealed record ServiceRegistrationJson
{
    [JsonPropertyName("interface")]
    public string? Interface { get; init; }

    [JsonPropertyName("implementations")]
    public List<ImplementationJson>? Implementations { get; init; }
}

internal sealed record ImplementationJson
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("alias")]
    public string? Alias { get; init; }
}

internal sealed record LifecycleConfigJson
{
    [JsonPropertyName("supportsHotReload")]
    public bool? SupportsHotReload { get; init; }

    [JsonPropertyName("requiresRestartOnUnload")]
    public bool? RequiresRestartOnUnload { get; init; }
}
