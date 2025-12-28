using ExperimentFramework.Configuration.Building;
using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using ExperimentFramework.Decorators;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Configuration.Extensions.Handlers;

/// <summary>
/// Handler for custom decorator types loaded via type resolution.
/// </summary>
internal sealed class CustomDecoratorHandler : IConfigurationDecoratorHandler
{
    private readonly ITypeResolver _typeResolver;

    public CustomDecoratorHandler(ITypeResolver typeResolver)
    {
        _typeResolver = typeResolver ?? throw new ArgumentNullException(nameof(typeResolver));
    }

    public string DecoratorType => "custom";

    public void Apply(ExperimentFrameworkBuilder builder, DecoratorConfig config, ILogger? logger)
    {
        if (string.IsNullOrEmpty(config.TypeName))
        {
            logger?.LogWarning("Custom decorator missing typeName, skipping");
            return;
        }

        try
        {
            var factoryType = _typeResolver.Resolve(config.TypeName);
            if (Activator.CreateInstance(factoryType) is IExperimentDecoratorFactory factory)
            {
                builder.AddDecoratorFactory(factory);
            }
            else
            {
                logger?.LogWarning("Type '{Type}' does not implement IExperimentDecoratorFactory",
                    config.TypeName);
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to create custom decorator '{Type}'", config.TypeName);
        }
    }

    public IEnumerable<ConfigurationValidationError> Validate(DecoratorConfig config, string path)
    {
        if (string.IsNullOrWhiteSpace(config.TypeName))
        {
            yield return ConfigurationValidationError.Error(
                $"{path}.typeName",
                "Type name is required for custom decorators");
        }
    }
}
