using System.Reflection;
using System.Text.Json;
using ExperimentFramework.Plugins.Abstractions;

namespace ExperimentFramework.Plugins.Manifest;

/// <summary>
/// Loads plugin manifests from various sources.
/// </summary>
public sealed class ManifestLoader
{
    private const string EmbeddedManifestName = "plugin.manifest.json";
    private const int DefaultMaxSize = 1024 * 1024; // 1MB
    private const int DefaultMaxDepth = 32;

    private readonly int _maxManifestSize;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Creates a new manifest loader with default settings.
    /// </summary>
    public ManifestLoader() : this(DefaultMaxSize, DefaultMaxDepth)
    {
    }

    /// <summary>
    /// Creates a new manifest loader with custom size and depth limits.
    /// </summary>
    /// <param name="maxManifestSize">Maximum manifest size in bytes. 0 for unlimited.</param>
    /// <param name="maxJsonDepth">Maximum JSON nesting depth.</param>
    public ManifestLoader(int maxManifestSize, int maxJsonDepth)
    {
        _maxManifestSize = maxManifestSize > 0 ? maxManifestSize : int.MaxValue;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            MaxDepth = maxJsonDepth > 0 ? maxJsonDepth : DefaultMaxDepth
        };
    }

    /// <summary>
    /// Loads a manifest for the specified plugin assembly.
    /// Tries in order: embedded resource, adjacent file, assembly attributes, then creates default.
    /// </summary>
    /// <param name="assembly">The plugin assembly.</param>
    /// <param name="assemblyPath">The path to the assembly file.</param>
    /// <returns>The loaded manifest.</returns>
    public IPluginManifest Load(Assembly assembly, string assemblyPath)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);

        // Try embedded resource first
        if (TryLoadFromEmbeddedResource(assembly, out var manifest))
        {
            return manifest;
        }

        // Try adjacent file
        if (TryLoadFromAdjacentFile(assemblyPath, out manifest))
        {
            return manifest;
        }

        // Try assembly attributes
        if (TryLoadFromAttributes(assembly, out manifest))
        {
            return manifest;
        }

        // Create default manifest from assembly info
        return CreateDefaultManifest(assembly);
    }

    /// <summary>
    /// Attempts to load a manifest from an embedded resource.
    /// </summary>
    public bool TryLoadFromEmbeddedResource(Assembly assembly, out IPluginManifest manifest)
    {
        manifest = null!;

        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(EmbeddedManifestName, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            return false;
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return false;
        }

        try
        {
            // Validate size limit
            if (stream.Length > _maxManifestSize)
            {
                throw new InvalidOperationException(
                    $"Embedded manifest exceeds maximum size of {_maxManifestSize} bytes. Actual: {stream.Length}");
            }

            var json = JsonSerializer.Deserialize<PluginManifestJson>(stream, _jsonOptions);
            if (json is not null)
            {
                manifest = json.ToManifest();
                return true;
            }
        }
        catch (JsonException)
        {
            // Invalid JSON, fall through to next method
        }

        return false;
    }

    /// <summary>
    /// Attempts to load a manifest from an adjacent file.
    /// Looks for {AssemblyName}.plugin.json next to the assembly.
    /// </summary>
    public bool TryLoadFromAdjacentFile(string assemblyPath, out IPluginManifest manifest)
    {
        manifest = null!;

        var directory = Path.GetDirectoryName(assemblyPath);
        if (string.IsNullOrEmpty(directory))
        {
            return false;
        }

        var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        var manifestPath = Path.Combine(directory, $"{assemblyName}.plugin.json");

        if (!File.Exists(manifestPath))
        {
            return false;
        }

        try
        {
            // Validate file size before reading
            var fileInfo = new FileInfo(manifestPath);
            if (fileInfo.Length > _maxManifestSize)
            {
                throw new InvalidOperationException(
                    $"Manifest file exceeds maximum size of {_maxManifestSize} bytes. Actual: {fileInfo.Length}");
            }

            var jsonContent = File.ReadAllText(manifestPath);
            var json = JsonSerializer.Deserialize<PluginManifestJson>(jsonContent, _jsonOptions);
            if (json is not null)
            {
                manifest = json.ToManifest();
                return true;
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // Invalid JSON or file access issue, fall through
        }

        return false;
    }

    /// <summary>
    /// Attempts to load manifest information from assembly attributes.
    /// </summary>
    public bool TryLoadFromAttributes(Assembly assembly, out IPluginManifest manifest)
    {
        manifest = null!;

        var pluginAttribute = assembly.GetCustomAttribute<PluginManifestAttribute>();
        if (pluginAttribute is null)
        {
            return false;
        }

        var isolationAttribute = assembly.GetCustomAttribute<PluginIsolationAttribute>();
        var serviceAttributes = assembly.GetCustomAttributes<PluginServiceAttribute>().ToList();

        var isolation = new PluginIsolationConfig
        {
            Mode = isolationAttribute?.Mode ?? PluginIsolationMode.Shared,
            SharedAssemblies = isolationAttribute?.SharedAssemblies ?? []
        };

        // Parse service registrations from attributes
        var services = serviceAttributes.Select(sa => new PluginServiceRegistration
        {
            Interface = sa.InterfaceName,
            Implementations = sa.Implementations
                .Select(ParseImplementation)
                .ToList()
        }).ToList();

        manifest = new PluginManifest
        {
            ManifestVersion = "1.0",
            Id = pluginAttribute.Id,
            Name = pluginAttribute.Name ?? pluginAttribute.Id,
            Version = pluginAttribute.Version ?? GetAssemblyVersion(assembly),
            Description = pluginAttribute.Description,
            Isolation = isolation,
            Services = services,
            Lifecycle = new PluginLifecycleConfig
            {
                SupportsHotReload = pluginAttribute.SupportsHotReload
            }
        };

        return true;
    }

    private static PluginImplementation ParseImplementation(string impl)
    {
        var colonIndex = impl.LastIndexOf(':');
        if (colonIndex > 0 && colonIndex < impl.Length - 1)
        {
            return new PluginImplementation
            {
                Type = impl.Substring(0, colonIndex),
                Alias = impl.Substring(colonIndex + 1)
            };
        }

        return new PluginImplementation
        {
            Type = impl,
            Alias = null
        };
    }

    private static PluginManifest CreateDefaultManifest(Assembly assembly)
    {
        var name = assembly.GetName();
        return PluginManifest.CreateDefault(
            name.Name ?? "Unknown",
            GetAssemblyVersion(assembly));
    }

    private static string GetAssemblyVersion(Assembly assembly)
    {
        var version = assembly.GetName().Version;
        return version?.ToString(3) ?? "1.0.0";
    }
}

/// <summary>
/// Assembly attribute for declaring plugin manifest metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class PluginManifestAttribute(string id) : Attribute
{
    /// <summary>
    /// Gets the unique identifier for the plugin.
    /// </summary>
    public string Id { get; } = id;

    /// <summary>
    /// Gets or sets the display name of the plugin.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or sets the version of the plugin.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Gets or sets the description of the plugin.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets whether the plugin supports hot reload.
    /// </summary>
    public bool SupportsHotReload { get; init; } = true;
}

/// <summary>
/// Assembly attribute for declaring plugin isolation requirements.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class PluginIsolationAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the isolation mode.
    /// </summary>
    public PluginIsolationMode Mode { get; init; } = PluginIsolationMode.Shared;

    /// <summary>
    /// Gets or sets the assemblies to share with the host.
    /// </summary>
    public string[] SharedAssemblies { get; init; } = [];
}

/// <summary>
/// Assembly attribute for declaring plugin service registrations.
/// This attribute is generated by the source generator.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class PluginServiceAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the interface name for this service.
    /// </summary>
    public required string InterfaceName { get; init; }

    /// <summary>
    /// Gets or sets the implementations in "FullTypeName:alias" format.
    /// </summary>
    public required string[] Implementations { get; init; }
}

/// <summary>
/// Optional class-level attribute for customizing plugin implementation registration.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class PluginImplementationAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the alias for this implementation.
    /// If not specified, derived from class name using kebab-case.
    /// </summary>
    public string? Alias { get; init; }

    /// <summary>
    /// Gets or sets the service interface this implementation provides.
    /// If not specified, all non-system interfaces are registered.
    /// </summary>
    public Type? ServiceInterface { get; init; }

    /// <summary>
    /// Gets or sets whether to exclude this class from auto-discovery.
    /// </summary>
    public bool Exclude { get; init; }
}

/// <summary>
/// Optional assembly-level attribute for customizing plugin manifest generation.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class GeneratePluginManifestAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the plugin ID. Defaults to assembly name.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Gets or sets the plugin display name. Defaults to assembly name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or sets the plugin description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the isolation mode. Defaults to Shared.
    /// </summary>
    public PluginIsolationMode IsolationMode { get; init; } = PluginIsolationMode.Shared;

    /// <summary>
    /// Gets or sets assemblies to share with the host.
    /// </summary>
    public string[]? SharedAssemblies { get; init; }

    /// <summary>
    /// Gets or sets whether the plugin supports hot reload.
    /// </summary>
    public bool SupportsHotReload { get; init; } = true;
}
