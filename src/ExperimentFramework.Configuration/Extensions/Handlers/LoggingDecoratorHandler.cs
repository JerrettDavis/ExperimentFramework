using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Configuration.Extensions.Handlers;

/// <summary>
/// Handler for the built-in logging decorator configuration.
/// </summary>
internal sealed class LoggingDecoratorHandler : IConfigurationDecoratorHandler
{
    public string DecoratorType => "logging";

    public void Apply(ExperimentFrameworkBuilder builder, DecoratorConfig config, ILogger? logger)
    {
        builder.AddLogger(l =>
        {
            if (config.Options != null)
            {
                if (GetBoolOption(config.Options, "benchmarks"))
                {
                    l.AddBenchmarks();
                }
                if (GetBoolOption(config.Options, "errorLogging"))
                {
                    l.AddErrorLogging();
                }
            }
        });
    }

    public IEnumerable<ConfigurationValidationError> Validate(DecoratorConfig config, string path)
    {
        // Logging decorator has no required fields
        yield break;
    }

    private static bool GetBoolOption(Dictionary<string, object> options, string key)
    {
        if (options.TryGetValue(key, out var value))
        {
            return value switch
            {
                bool b => b,
                string s => bool.TryParse(s, out var result) && result,
                _ => false
            };
        }
        return false;
    }
}
