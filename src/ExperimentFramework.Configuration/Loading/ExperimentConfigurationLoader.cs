using ExperimentFramework.Configuration.Exceptions;
using ExperimentFramework.Configuration.Models;
using Microsoft.Extensions.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ExperimentFramework.Configuration.Loading;

/// <summary>
/// Default implementation of configuration loading.
/// </summary>
public sealed class ExperimentConfigurationLoader : IExperimentConfigurationLoader
{
    private readonly ConfigurationFileDiscovery _fileDiscovery = new();

    private readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <inheritdoc />
    public ExperimentFrameworkConfigurationRoot Load(
        IConfiguration configuration,
        ExperimentFrameworkConfigurationOptions options)
    {
        var result = new ExperimentFrameworkConfigurationRoot
        {
            Settings = new FrameworkSettingsConfig(),
            Decorators = [],
            Trials = [],
            Experiments = [],
            ConfigurationPaths = []
        };

        // Load from IConfiguration section
        var section = configuration.GetSection(options.ConfigurationSectionName);
        if (section.Exists())
        {
            MergeFromConfiguration(result, section);
        }

        // Determine base path
        var basePath = options.BasePath ?? Directory.GetCurrentDirectory();

        // Add additional paths from configuration
        if (result.ConfigurationPaths?.Count > 0)
        {
            foreach (var path in result.ConfigurationPaths)
            {
                if (!options.AdditionalPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
                {
                    options.AdditionalPaths.Add(path);
                }
            }
        }

        // Discover and load files
        var files = _fileDiscovery.DiscoverFiles(basePath, options);
        foreach (var file in files)
        {
            try
            {
                var fileConfig = LoadFromFile(file);
                Merge(result, fileConfig);
            }
            catch (Exception ex)
            {
                throw new ConfigurationLoadException(file, "Failed to parse configuration file", ex);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public ExperimentFrameworkConfigurationRoot LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new ConfigurationLoadException(filePath, "File not found");
        }

        var content = File.ReadAllText(filePath);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".yaml" or ".yml" => ParseYaml(content, filePath),
            ".json" => ParseJson(content, filePath),
            _ => throw new ConfigurationLoadException(filePath, $"Unsupported file extension: {extension}")
        };
    }

    private ExperimentFrameworkConfigurationRoot ParseYaml(string content, string filePath)
    {
        try
        {
            // Try to parse as wrapped format first (experimentFramework: { ... })
            var wrapped = _yamlDeserializer.Deserialize<Dictionary<string, ExperimentFrameworkConfigurationRoot>>(content);
            if (wrapped != null && wrapped.TryGetValue("experimentFramework", out var config))
            {
                return config;
            }

            // Try as direct format
            return _yamlDeserializer.Deserialize<ExperimentFrameworkConfigurationRoot>(content)
                   ?? new ExperimentFrameworkConfigurationRoot();
        }
        catch (Exception ex)
        {
            throw new ConfigurationLoadException(filePath, "YAML parsing failed", ex);
        }
    }

    private ExperimentFrameworkConfigurationRoot ParseJson(string content, string filePath)
    {
        try
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            // Try wrapped format first
            var wrapped = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, ExperimentFrameworkConfigurationRoot>>(content, options);
            if (wrapped != null && wrapped.TryGetValue("experimentFramework", out var config))
            {
                return config;
            }
            if (wrapped != null && wrapped.TryGetValue("ExperimentFramework", out config))
            {
                return config;
            }

            // Try direct format
            return System.Text.Json.JsonSerializer.Deserialize<ExperimentFrameworkConfigurationRoot>(content, options)
                   ?? new ExperimentFrameworkConfigurationRoot();
        }
        catch (Exception ex)
        {
            throw new ConfigurationLoadException(filePath, "JSON parsing failed", ex);
        }
    }

    private static void MergeFromConfiguration(ExperimentFrameworkConfigurationRoot result, IConfigurationSection section)
    {
        // Bind settings
        var settingsSection = section.GetSection("settings");
        if (settingsSection.Exists())
        {
            result.Settings ??= new FrameworkSettingsConfig();
            settingsSection.Bind(result.Settings);
        }

        // Bind configuration paths
        var pathsSection = section.GetSection("configurationPaths");
        if (pathsSection.Exists())
        {
            result.ConfigurationPaths ??= [];
            var paths = pathsSection.Get<List<string>>();
            if (paths != null)
            {
                result.ConfigurationPaths.AddRange(paths);
            }
        }

        // Note: Complex nested objects (trials, experiments, decorators) are better handled
        // via file-based configuration for YAML support
    }

    private static void Merge(ExperimentFrameworkConfigurationRoot target, ExperimentFrameworkConfigurationRoot source)
    {
        // Merge settings (source overrides target)
        if (source.Settings != null)
        {
            target.Settings ??= new FrameworkSettingsConfig();
            if (!string.IsNullOrEmpty(source.Settings.ProxyStrategy))
            {
                target.Settings.ProxyStrategy = source.Settings.ProxyStrategy;
            }
            if (!string.IsNullOrEmpty(source.Settings.NamingConvention))
            {
                target.Settings.NamingConvention = source.Settings.NamingConvention;
            }
        }

        // Merge decorators (append)
        if (source.Decorators != null)
        {
            target.Decorators ??= [];
            target.Decorators.AddRange(source.Decorators);
        }

        // Merge trials (append)
        if (source.Trials != null)
        {
            target.Trials ??= [];
            target.Trials.AddRange(source.Trials);
        }

        // Merge experiments (append, but warn on duplicate names)
        if (source.Experiments != null)
        {
            target.Experiments ??= [];
            foreach (var experiment in source.Experiments)
            {
                // Remove existing with same name (last one wins)
                target.Experiments.RemoveAll(e =>
                    e.Name.Equals(experiment.Name, StringComparison.OrdinalIgnoreCase));
                target.Experiments.Add(experiment);
            }
        }

        // Merge configuration paths (append unique)
        if (source.ConfigurationPaths != null)
        {
            target.ConfigurationPaths ??= [];
            foreach (var path in source.ConfigurationPaths)
            {
                if (!target.ConfigurationPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
                {
                    target.ConfigurationPaths.Add(path);
                }
            }
        }
    }
}
