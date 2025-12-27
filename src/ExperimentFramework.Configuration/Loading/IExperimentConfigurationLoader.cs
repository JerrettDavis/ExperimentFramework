using ExperimentFramework.Configuration.Models;
using Microsoft.Extensions.Configuration;

namespace ExperimentFramework.Configuration.Loading;

/// <summary>
/// Loads experiment configuration from various sources.
/// </summary>
public interface IExperimentConfigurationLoader
{
    /// <summary>
    /// Loads configuration from the specified IConfiguration and discovered files.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="options">Loading options.</param>
    /// <returns>The merged configuration root.</returns>
    ExperimentFrameworkConfigurationRoot Load(
        IConfiguration configuration,
        ExperimentFrameworkConfigurationOptions options);

    /// <summary>
    /// Loads configuration from a specific file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>The configuration from the file.</returns>
    ExperimentFrameworkConfigurationRoot LoadFromFile(string filePath);
}
