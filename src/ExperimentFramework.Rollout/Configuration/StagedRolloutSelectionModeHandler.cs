using ExperimentFramework.Configuration.Extensions;
using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Rollout.Configuration;

/// <summary>
/// Configuration handler for the staged rollout selection mode.
/// This handler allows time-based staged rollouts to be configured via YAML/JSON configuration files.
/// </summary>
public sealed class StagedRolloutSelectionModeHandler : IConfigurationSelectionModeHandler
{
    /// <inheritdoc />
    public string ModeType => "stagedRollout";

    /// <inheritdoc />
    public void Apply<TService>(ServiceExperimentBuilder<TService> builder, SelectionModeConfig config, ILogger? logger)
        where TService : class
    {
        var options = ParseOptions(config);
        builder.UsingCustomMode(StagedRolloutModes.StagedRollout, config.SelectorName);

        logger?.LogDebug(
            "Configured staged rollout selection mode for {ServiceType} with {StageCount} stages",
            typeof(TService).Name,
            options.Stages.Count);
    }

    /// <inheritdoc />
    public IEnumerable<ConfigurationValidationError> Validate(SelectionModeConfig config, string path)
    {
        if (config.Options == null)
        {
            yield break;
        }

        if (config.Options.TryGetValue("stages", out var stagesObj) && stagesObj is List<object> stages)
        {
            for (var i = 0; i < stages.Count; i++)
            {
                if (stages[i] is not Dictionary<string, object> stage)
                {
                    yield return ConfigurationValidationError.Error(
                        $"{path}.options.stages[{i}]",
                        "Stage must be an object with 'startsAt' and 'percentage' properties");
                    continue;
                }

                if (!stage.ContainsKey("startsAt"))
                {
                    yield return ConfigurationValidationError.Error(
                        $"{path}.options.stages[{i}].startsAt",
                        "Stage must have a 'startsAt' datetime");
                }

                if (!stage.ContainsKey("percentage"))
                {
                    yield return ConfigurationValidationError.Error(
                        $"{path}.options.stages[{i}].percentage",
                        "Stage must have a 'percentage' value");
                }
                else if (TryGetIntOption(stage, "percentage", out var percentage))
                {
                    if (percentage < 0 || percentage > 100)
                    {
                        yield return ConfigurationValidationError.Error(
                            $"{path}.options.stages[{i}].percentage",
                            "Percentage must be between 0 and 100");
                    }
                }
            }
        }
    }

    private static StagedRolloutOptions ParseOptions(SelectionModeConfig config)
    {
        var result = new StagedRolloutOptions();

        if (config.Options == null)
            return result;

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

        if (config.Options.TryGetValue("stages", out var stagesObj) && stagesObj is List<object> stages)
        {
            foreach (var stageObj in stages)
            {
                if (stageObj is Dictionary<string, object> stage)
                {
                    var rolloutStage = new RolloutStage();

                    if (TryGetDateTimeOffsetOption(stage, "startsAt", out var startsAt))
                    {
                        rolloutStage.StartsAt = startsAt;
                    }

                    if (TryGetIntOption(stage, "percentage", out var percentage))
                    {
                        rolloutStage.Percentage = Math.Clamp(percentage, 0, 100);
                    }

                    if (stage.TryGetValue("description", out var desc) && desc is string descStr)
                    {
                        rolloutStage.Description = descStr;
                    }

                    result.Stages.Add(rolloutStage);
                }
            }
        }

        return result;
    }

    private static bool TryGetIntOption(Dictionary<string, object> options, string key, out int result)
    {
        result = 0;
        if (!options.TryGetValue(key, out var value))
            return false;

        return value switch
        {
            int i => (result = i) == i,
            long l => (result = (int)l) == (int)l,
            string s => int.TryParse(s, out result),
            _ => false
        };
    }

    private static bool TryGetDateTimeOffsetOption(Dictionary<string, object> options, string key, out DateTimeOffset result)
    {
        result = default;
        if (!options.TryGetValue(key, out var value))
            return false;

        return value switch
        {
            DateTimeOffset dto => (result = dto) == dto,
            DateTime dt => (result = new DateTimeOffset(dt)) == result,
            string s => DateTimeOffset.TryParse(s, out result),
            _ => false
        };
    }
}
