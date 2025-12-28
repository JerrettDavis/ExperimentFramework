using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Configuration.Extensions.Handlers;

/// <summary>
/// Handler for custom selection modes registered via ISelectionModeProviderFactory.
/// </summary>
internal sealed class CustomSelectionModeHandler : IConfigurationSelectionModeHandler
{
    public string ModeType => "custom";

    public void Apply<TService>(ServiceExperimentBuilder<TService> builder, SelectionModeConfig config, ILogger? logger)
        where TService : class
    {
        if (string.IsNullOrWhiteSpace(config.ModeIdentifier))
        {
            logger?.LogWarning("Custom selection mode missing modeIdentifier, skipping");
            return;
        }

        builder.UsingCustomMode(config.ModeIdentifier, config.SelectorName);
    }

    public IEnumerable<ConfigurationValidationError> Validate(SelectionModeConfig config, string path)
    {
        if (string.IsNullOrWhiteSpace(config.ModeIdentifier))
        {
            yield return ConfigurationValidationError.Error(
                $"{path}.modeIdentifier",
                "Mode identifier is required for custom selection mode");
        }
    }
}
