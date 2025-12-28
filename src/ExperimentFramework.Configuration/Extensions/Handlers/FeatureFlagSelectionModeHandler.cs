using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Configuration.Extensions.Handlers;

/// <summary>
/// Handler for the built-in featureFlag selection mode.
/// </summary>
internal sealed class FeatureFlagSelectionModeHandler : IConfigurationSelectionModeHandler
{
    public string ModeType => "featureFlag";

    public void Apply<TService>(ServiceExperimentBuilder<TService> builder, SelectionModeConfig config, ILogger? logger)
        where TService : class
    {
        builder.UsingFeatureFlag(config.FlagName);
    }

    public IEnumerable<ConfigurationValidationError> Validate(SelectionModeConfig config, string path)
    {
        if (string.IsNullOrWhiteSpace(config.FlagName))
        {
            yield return ConfigurationValidationError.Error(
                $"{path}.flagName",
                "Flag name is required for featureFlag selection mode");
        }
    }
}
