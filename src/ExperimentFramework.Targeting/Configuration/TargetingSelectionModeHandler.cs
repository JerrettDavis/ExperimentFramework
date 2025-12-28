using ExperimentFramework.Configuration.Extensions;
using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Targeting.Configuration;

/// <summary>
/// Configuration handler for the targeting selection mode.
/// </summary>
public sealed class TargetingSelectionModeHandler : IConfigurationSelectionModeHandler
{
    /// <inheritdoc />
    public string ModeType => "targeting";

    /// <inheritdoc />
    public void Apply<TService>(ServiceExperimentBuilder<TService> builder, SelectionModeConfig config, ILogger? logger)
        where TService : class
    {
        builder.UsingCustomMode(TargetingModes.Targeting, config.SelectorName);

        logger?.LogDebug(
            "Configured targeting selection mode for {ServiceType}",
            typeof(TService).Name);
    }

    /// <inheritdoc />
    public IEnumerable<ConfigurationValidationError> Validate(SelectionModeConfig config, string path)
    {
        // Basic validation - targeting rules are typically configured programmatically
        yield break;
    }
}
