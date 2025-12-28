using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using ExperimentFramework.Models;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Configuration.Extensions.Handlers;

/// <summary>
/// Handler for the built-in timeout decorator configuration.
/// </summary>
internal sealed class TimeoutDecoratorHandler : IConfigurationDecoratorHandler
{
    public string DecoratorType => "timeout";

    public void Apply(ExperimentFrameworkBuilder builder, DecoratorConfig config, ILogger? logger)
    {
        var timeout = TimeSpan.FromSeconds(30);
        var onTimeout = TimeoutAction.FallbackToDefault;
        string? fallbackKey = null;

        if (config.Options != null)
        {
            if (TryGetTimeSpanOption(config.Options, "timeout", out var t))
            {
                timeout = t;
            }

            if (config.Options.TryGetValue("onTimeout", out var action) && action is string actionStr)
            {
                onTimeout = actionStr.ToLowerInvariant() switch
                {
                    "throw" or "throwexception" => TimeoutAction.ThrowException,
                    "fallbacktodefault" => TimeoutAction.FallbackToDefault,
                    "fallbacktospecifictrial" => TimeoutAction.FallbackToSpecificTrial,
                    _ => TimeoutAction.FallbackToDefault
                };
            }

            if (config.Options.TryGetValue("fallbackTrialKey", out var key) && key is string keyStr)
            {
                fallbackKey = keyStr;
            }
        }

        builder.WithTimeout(timeout, onTimeout, fallbackKey);
    }

    public IEnumerable<ConfigurationValidationError> Validate(DecoratorConfig config, string path)
    {
        if (config.Options != null)
        {
            if (config.Options.TryGetValue("timeout", out var timeoutValue))
            {
                if (timeoutValue is string str && !TimeSpan.TryParse(str, out _))
                {
                    yield return ConfigurationValidationError.Error(
                        $"{path}.options.timeout",
                        $"Invalid timeout format: '{str}'. Expected TimeSpan format (e.g., '00:00:30').");
                }
            }

            if (config.Options.TryGetValue("onTimeout", out var action) && action is string actionStr)
            {
                var validActions = new[] { "throw", "throwexception", "fallbacktodefault", "fallbacktospecifictrial" };
                if (!validActions.Contains(actionStr.ToLowerInvariant()))
                {
                    yield return ConfigurationValidationError.Warning(
                        $"{path}.options.onTimeout",
                        $"Unknown timeout action: '{actionStr}'. Will default to 'fallbackToDefault'.");
                }
            }
        }
    }

    private static bool TryGetTimeSpanOption(Dictionary<string, object> options, string key, out TimeSpan result)
    {
        result = default;
        if (options.TryGetValue(key, out var value))
        {
            return value switch
            {
                TimeSpan ts => (result = ts) == ts,
                string s => TimeSpan.TryParse(s, out result),
                _ => false
            };
        }
        return false;
    }
}
