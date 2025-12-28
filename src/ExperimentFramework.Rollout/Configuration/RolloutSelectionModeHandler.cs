using ExperimentFramework.Configuration.Extensions;
using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Rollout.Configuration;

/// <summary>
/// Configuration handler for the rollout selection mode.
/// This handler allows percentage-based rollouts to be configured via YAML/JSON configuration files.
/// </summary>
public sealed class RolloutSelectionModeHandler : IConfigurationSelectionModeHandler
{
    /// <inheritdoc />
    public string ModeType => "rollout";

    /// <inheritdoc />
    public void Apply<TService>(ServiceExperimentBuilder<TService> builder, SelectionModeConfig config, ILogger? logger)
        where TService : class
    {
        var options = ParseOptions(config);
        builder.UsingCustomMode(RolloutModes.Rollout, config.SelectorName);

        logger?.LogDebug(
            "Configured rollout selection mode for {ServiceType} with {Percentage}%",
            typeof(TService).Name,
            options.Percentage);
    }

    /// <inheritdoc />
    public IEnumerable<ConfigurationValidationError> Validate(SelectionModeConfig config, string path)
    {
        if (TryGetIntOption(config.Options, "percentage", out var percentage))
        {
            if (percentage < 0 || percentage > 100)
            {
                yield return ConfigurationValidationError.Error(
                    $"{path}.options.percentage",
                    "Percentage must be between 0 and 100");
            }
        }
    }

    private static RolloutOptions ParseOptions(SelectionModeConfig config)
    {
        var result = new RolloutOptions();

        if (config.Options == null)
            return result;

        if (TryGetIntOption(config.Options, "percentage", out var percentage))
        {
            result.Percentage = Math.Clamp(percentage, 0, 100);
        }

        if (config.Options.TryGetValue("includedKey", out var includedKey) && includedKey is string includedStr)
        {
            result.IncludedKey = includedStr;
        }

        if (config.Options.TryGetValue("excludedKey", out var excludedKey) && excludedKey is string excludedStr)
        {
            result.ExcludedKey = excludedStr;
        }

        if (config.Options.TryGetValue("seed", out var seed) && seed is string seedStr)
        {
            result.Seed = seedStr;
        }

        return result;
    }

    private static bool TryGetIntOption(Dictionary<string, object>? options, string key, out int result)
    {
        result = 0;
        if (options == null || !options.TryGetValue(key, out var value))
            return false;

        return value switch
        {
            int i => (result = i) == i,
            long l => (result = (int)l) == (int)l,
            string s => int.TryParse(s, out result),
            _ => false
        };
    }
}
