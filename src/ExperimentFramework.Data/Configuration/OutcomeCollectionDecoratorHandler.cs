using ExperimentFramework.Configuration.Extensions;
using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using ExperimentFramework.Data.Recording;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Data.Configuration;

/// <summary>
/// Configuration handler for the outcome collection decorator.
/// This handler allows outcome collection to be configured via YAML/JSON configuration files.
/// </summary>
public sealed class OutcomeCollectionDecoratorHandler : IConfigurationDecoratorHandler
{
    /// <inheritdoc />
    public string DecoratorType => "outcomeCollection";

    /// <inheritdoc />
    public void Apply(ExperimentFrameworkBuilder builder, DecoratorConfig config, ILogger? logger)
    {
        var options = ParseOptions(config.Options);
        builder.WithOutcomeCollection(opts =>
        {
            opts.AutoGenerateIds = options.AutoGenerateIds;
            opts.AutoSetTimestamps = options.AutoSetTimestamps;
            opts.CollectDuration = options.CollectDuration;
            opts.CollectErrors = options.CollectErrors;
            opts.DurationMetricName = options.DurationMetricName;
            opts.ErrorMetricName = options.ErrorMetricName;
            opts.SuccessMetricName = options.SuccessMetricName;
            opts.EnableBatching = options.EnableBatching;
            opts.MaxBatchSize = options.MaxBatchSize;
            opts.BatchFlushInterval = options.BatchFlushInterval;
        });
    }

    /// <inheritdoc />
    public IEnumerable<ConfigurationValidationError> Validate(DecoratorConfig config, string path)
    {
        if (config.Options == null)
        {
            // No options is fine - defaults will be used
            yield break;
        }

        // Validate max batch size
        if (TryGetIntOption(config.Options, "maxBatchSize", out var batchSize))
        {
            if (batchSize <= 0)
            {
                yield return ConfigurationValidationError.Error(
                    $"{path}.options.maxBatchSize",
                    "Max batch size must be positive");
            }
        }

        // Validate batch flush interval
        if (TryGetTimeSpanOption(config.Options, "batchFlushInterval", out var interval))
        {
            if (interval <= TimeSpan.Zero)
            {
                yield return ConfigurationValidationError.Error(
                    $"{path}.options.batchFlushInterval",
                    "Batch flush interval must be positive");
            }
        }
    }

    private static OutcomeRecorderOptions ParseOptions(Dictionary<string, object>? options)
    {
        var result = new OutcomeRecorderOptions();

        if (options == null)
            return result;

        if (TryGetBoolOption(options, "autoGenerateIds", out var autoGen))
        {
            result.AutoGenerateIds = autoGen;
        }

        if (TryGetBoolOption(options, "autoSetTimestamps", out var autoTs))
        {
            result.AutoSetTimestamps = autoTs;
        }

        if (TryGetBoolOption(options, "collectDuration", out var collectDur))
        {
            result.CollectDuration = collectDur;
        }

        if (TryGetBoolOption(options, "collectErrors", out var collectErr))
        {
            result.CollectErrors = collectErr;
        }

        if (options.TryGetValue("durationMetricName", out var durName) && durName is string durStr)
        {
            result.DurationMetricName = durStr;
        }

        if (options.TryGetValue("errorMetricName", out var errName) && errName is string errStr)
        {
            result.ErrorMetricName = errStr;
        }

        if (options.TryGetValue("successMetricName", out var succName) && succName is string succStr)
        {
            result.SuccessMetricName = succStr;
        }

        if (TryGetBoolOption(options, "enableBatching", out var batch))
        {
            result.EnableBatching = batch;
        }

        if (TryGetIntOption(options, "maxBatchSize", out var batchSize))
        {
            result.MaxBatchSize = batchSize;
        }

        if (TryGetTimeSpanOption(options, "batchFlushInterval", out var interval))
        {
            result.BatchFlushInterval = interval;
        }

        return result;
    }

    private static bool TryGetBoolOption(Dictionary<string, object> options, string key, out bool result)
    {
        result = false;
        if (!options.TryGetValue(key, out var value))
            return false;

        return value switch
        {
            bool b => (result = b) || true,
            string s => bool.TryParse(s, out result),
            _ => false
        };
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

    private static bool TryGetTimeSpanOption(Dictionary<string, object> options, string key, out TimeSpan result)
    {
        result = default;
        if (!options.TryGetValue(key, out var value))
            return false;

        return value switch
        {
            TimeSpan ts => (result = ts) == ts,
            string s => TimeSpan.TryParse(s, out result),
            _ => false
        };
    }
}
