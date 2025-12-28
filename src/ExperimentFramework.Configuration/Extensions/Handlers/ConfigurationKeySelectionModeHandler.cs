using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Configuration.Extensions.Handlers;

/// <summary>
/// Handler for the built-in configurationKey selection mode.
/// </summary>
internal sealed class ConfigurationKeySelectionModeHandler : IConfigurationSelectionModeHandler
{
    public string ModeType => "configurationKey";

    public void Apply<TService>(ServiceExperimentBuilder<TService> builder, SelectionModeConfig config, ILogger? logger)
        where TService : class
    {
        builder.UsingConfigurationKey(config.Key);
    }

    public IEnumerable<ConfigurationValidationError> Validate(SelectionModeConfig config, string path)
    {
        if (string.IsNullOrWhiteSpace(config.Key))
        {
            yield return ConfigurationValidationError.Error(
                $"{path}.key",
                "Key is required for configurationKey selection mode");
        }
    }
}
