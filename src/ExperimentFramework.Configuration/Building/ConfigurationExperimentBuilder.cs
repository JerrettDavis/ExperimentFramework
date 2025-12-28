using System.Reflection;
using ExperimentFramework.Configuration.Activation;
using ExperimentFramework.Configuration.Exceptions;
using ExperimentFramework.Configuration.Extensions;
using ExperimentFramework.Configuration.Extensions.Handlers;
using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Naming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Configuration.Building;

/// <summary>
/// Builds an ExperimentFrameworkBuilder from configuration models.
/// </summary>
public sealed class ConfigurationExperimentBuilder
{
    private readonly ITypeResolver _typeResolver;
    private readonly ConfigurationExtensionRegistry? _extensionRegistry;
    private readonly ILogger<ConfigurationExperimentBuilder>? _logger;

    /// <summary>
    /// Creates a new configuration experiment builder.
    /// </summary>
    public ConfigurationExperimentBuilder(
        ITypeResolver typeResolver,
        ConfigurationExtensionRegistry? extensionRegistry = null,
        ILogger<ConfigurationExperimentBuilder>? logger = null)
    {
        _typeResolver = typeResolver;
        _extensionRegistry = extensionRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new configuration experiment builder (backward compatible).
    /// </summary>
    public ConfigurationExperimentBuilder(ITypeResolver typeResolver, ILogger<ConfigurationExperimentBuilder>? logger)
        : this(typeResolver, null, logger)
    {
    }

    /// <summary>
    /// Builds an ExperimentFrameworkBuilder from the configuration.
    /// </summary>
    public ExperimentFrameworkBuilder Build(ExperimentFrameworkConfigurationRoot config)
    {
        var builder = ExperimentFrameworkBuilder.Create();

        // Apply settings
        if (config.Settings != null)
        {
            ApplySettings(builder, config.Settings);
        }

        // Add decorators
        if (config.Decorators != null)
        {
            foreach (var decorator in config.Decorators)
            {
                AddDecorator(builder, decorator);
            }
        }

        // Add standalone trials
        if (config.Trials != null)
        {
            foreach (var trial in config.Trials)
            {
                AddTrial(builder, trial);
            }
        }

        // Add named experiments
        if (config.Experiments != null)
        {
            foreach (var experiment in config.Experiments)
            {
                AddExperiment(builder, experiment);
            }
        }

        return builder;
    }

    /// <summary>
    /// Merges configuration into an existing builder.
    /// </summary>
    public void MergeInto(ExperimentFrameworkBuilder builder, ExperimentFrameworkConfigurationRoot config)
    {
        // Note: Settings are not merged as they should be set programmatically first

        // Add decorators
        if (config.Decorators != null)
        {
            foreach (var decorator in config.Decorators)
            {
                AddDecorator(builder, decorator);
            }
        }

        // Add standalone trials
        if (config.Trials != null)
        {
            foreach (var trial in config.Trials)
            {
                AddTrial(builder, trial);
            }
        }

        // Add named experiments
        if (config.Experiments != null)
        {
            foreach (var experiment in config.Experiments)
            {
                AddExperiment(builder, experiment);
            }
        }
    }

    private void ApplySettings(ExperimentFrameworkBuilder builder, FrameworkSettingsConfig settings)
    {
        // Proxy strategy
        if (settings.ProxyStrategy.Equals("dispatchProxy", StringComparison.OrdinalIgnoreCase))
        {
            builder.UseDispatchProxy();
        }
        else
        {
            builder.UseSourceGenerators();
        }

        // Naming convention
        if (!string.IsNullOrEmpty(settings.NamingConvention) &&
            !settings.NamingConvention.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var conventionType = _typeResolver.Resolve(settings.NamingConvention);
                if (Activator.CreateInstance(conventionType) is IExperimentNamingConvention convention)
                {
                    builder.UseNamingConvention(convention);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load naming convention '{Convention}', using default",
                    settings.NamingConvention);
            }
        }
    }

    private void AddDecorator(ExperimentFrameworkBuilder builder, DecoratorConfig decorator)
    {
        // Try to find a registered handler first
        var handler = _extensionRegistry?.GetDecoratorHandler(decorator.Type);
        if (handler != null)
        {
            handler.Apply(builder, decorator, _logger);
            return;
        }

        // Fallback to built-in handlers when no registry is provided
        handler = GetBuiltInDecoratorHandler(decorator.Type);
        if (handler != null)
        {
            handler.Apply(builder, decorator, _logger);
            return;
        }

        // Log unknown decorator type
        _logger?.LogWarning(
            "Unknown decorator type '{Type}'. Register a handler via AddConfigurationDecoratorHandler<T>() or install an extension package that provides this decorator.",
            decorator.Type);
    }

    private IConfigurationDecoratorHandler? GetBuiltInDecoratorHandler(string decoratorType)
    {
        return decoratorType.ToLowerInvariant() switch
        {
            "logging" => new LoggingDecoratorHandler(),
            "timeout" => new TimeoutDecoratorHandler(),
            "custom" => new CustomDecoratorHandler(_typeResolver),
            _ => null
        };
    }

    private void AddTrial(ExperimentFrameworkBuilder builder, TrialConfig trial)
    {
        try
        {
            var serviceType = _typeResolver.Resolve(trial.ServiceType);

            // Get the Define<TService> method and make it generic
            var defineMethod = typeof(ExperimentFrameworkBuilder)
                .GetMethod(nameof(ExperimentFrameworkBuilder.Define))!
                .MakeGenericMethod(serviceType);

            // Create the configuration action using reflection
            _ = typeof(Action<>).MakeGenericType(
                typeof(ServiceExperimentBuilder<>).MakeGenericType(serviceType));

            var configureMethod = GetType()
                .GetMethod(nameof(CreateTrialConfigureAction), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(serviceType);

            var action = configureMethod.Invoke(this, [trial]);

            defineMethod.Invoke(builder, [action]);
        }
        catch (Exception ex)
        {
            throw new ExperimentConfigurationException(
                $"Failed to add trial for service type '{trial.ServiceType}'", ex);
        }
    }

    private Action<ServiceExperimentBuilder<TService>> CreateTrialConfigureAction<TService>(TrialConfig trial)
        where TService : class
    {
        return b =>
        {
            // Selection mode
            ConfigureSelectionMode(b, trial.SelectionMode);

            // Control
            AddControl(b, trial.Control);

            // Conditions
            if (trial.Conditions != null)
            {
                foreach (var condition in trial.Conditions)
                {
                    AddCondition(b, condition);
                }
            }

            // Error policy
            if (trial.ErrorPolicy != null)
            {
                ConfigureErrorPolicy(b, trial.ErrorPolicy);
            }

            // Activation
            if (trial.Activation != null)
            {
                ConfigureActivation(b, trial.Activation);
            }
        };
    }

    private void ConfigureSelectionMode<TService>(ServiceExperimentBuilder<TService> builder, SelectionModeConfig mode)
        where TService : class
    {
        // Try to find a registered handler first
        var handler = _extensionRegistry?.GetSelectionModeHandler(mode.Type);
        if (handler != null)
        {
            handler.Apply(builder, mode, _logger);
            return;
        }

        // Fallback to built-in handlers when no registry is provided
        handler = GetBuiltInSelectionModeHandler(mode.Type);
        if (handler != null)
        {
            handler.Apply(builder, mode, _logger);
            return;
        }

        // Log unknown selection mode type
        _logger?.LogWarning(
            "Unknown selection mode type '{Type}'. Register a handler via AddConfigurationSelectionModeHandler<T>() or install an extension package that provides this mode.",
            mode.Type);
    }

    private IConfigurationSelectionModeHandler? GetBuiltInSelectionModeHandler(string modeType)
    {
        return modeType.ToLowerInvariant() switch
        {
            "featureflag" => new FeatureFlagSelectionModeHandler(),
            "configurationkey" => new ConfigurationKeySelectionModeHandler(),
            "custom" => new CustomSelectionModeHandler(),
            _ => null
        };
    }

    private void AddControl<TService>(ServiceExperimentBuilder<TService> builder, ConditionConfig control)
        where TService : class
    {
        var implementationType = _typeResolver.Resolve(control.ImplementationType);

        // Call AddControl<TImpl>(key) using reflection
        var method = typeof(ServiceExperimentBuilder<TService>)
            .GetMethods()
            .First(m => m is { Name: "AddControl", IsGenericMethod: true } &&
                        m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType == typeof(string));

        var genericMethod = method.MakeGenericMethod(implementationType);
        genericMethod.Invoke(builder, [control.Key]);
    }

    private void AddCondition<TService>(ServiceExperimentBuilder<TService> builder, ConditionConfig condition)
        where TService : class
    {
        var implementationType = _typeResolver.Resolve(condition.ImplementationType);

        // Call AddCondition<TImpl>(key) using reflection
        var method = typeof(ServiceExperimentBuilder<TService>)
            .GetMethods()
            .First(m => m is { Name: "AddCondition", IsGenericMethod: true } &&
                        m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType == typeof(string));

        var genericMethod = method.MakeGenericMethod(implementationType);
        genericMethod.Invoke(builder, [condition.Key]);
    }

    private static void ConfigureErrorPolicy<TService>(ServiceExperimentBuilder<TService> builder, ErrorPolicyConfig policy)
        where TService : class
    {
        switch (policy.Type.ToLowerInvariant())
        {
            case "throw":
                builder.OnErrorThrow();
                break;

            case "fallbacktocontrol":
                builder.OnErrorFallbackToControl();
                break;

            case "fallbackto":
                builder.OnErrorFallbackTo(policy.FallbackKey!);
                break;

            case "tryinorder":
                builder.OnErrorTryInOrder(policy.FallbackKeys!.ToArray());
                break;

            case "tryany":
                builder.OnErrorTryAny();
                break;
        }
    }

    private void ConfigureActivation<TService>(ServiceExperimentBuilder<TService> builder, ActivationConfig activation)
        where TService : class
    {
        if (activation.From.HasValue)
        {
            builder.ActiveFrom(activation.From.Value);
        }

        if (activation.Until.HasValue)
        {
            builder.ActiveUntil(activation.Until.Value);
        }

        if (activation.Predicate != null)
        {
            var predicateType = _typeResolver.Resolve(activation.Predicate.Type);
            var predicate = CreateActivationPredicate(predicateType);
            builder.ActiveWhen(predicate);
        }
    }

    private Func<IServiceProvider, bool> CreateActivationPredicate(Type predicateType)
    {
        if (typeof(IActivationPredicate).IsAssignableFrom(predicateType))
        {
            return sp =>
            {
                var predicate = (IActivationPredicate)ActivatorUtilities.CreateInstance(sp, predicateType);
                return predicate.IsActive(sp);
            };
        }

        // Try to create as Func<IServiceProvider, bool>
        var instance = Activator.CreateInstance(predicateType);
        if (instance is Func<IServiceProvider, bool> func)
        {
            return func;
        }

        throw new TypeResolutionException(predicateType.FullName!,
            "Predicate type must implement IActivationPredicate or be Func<IServiceProvider, bool>");
    }

    private void AddExperiment(ExperimentFrameworkBuilder builder, ExperimentConfig experiment)
    {
        builder.Experiment(experiment.Name, exp =>
        {
            // Metadata
            if (experiment.Metadata != null)
            {
                foreach (var (key, value) in experiment.Metadata)
                {
                    exp.WithMetadata(key, value);
                }
            }

            // Activation
            if (experiment.Activation != null)
            {
                if (experiment.Activation.From.HasValue)
                {
                    exp.ActiveFrom(experiment.Activation.From.Value);
                }
                if (experiment.Activation.Until.HasValue)
                {
                    exp.ActiveUntil(experiment.Activation.Until.Value);
                }
                if (experiment.Activation.Predicate != null)
                {
                    var predicateType = _typeResolver.Resolve(experiment.Activation.Predicate.Type);
                    var predicate = CreateActivationPredicate(predicateType);
                    exp.ActiveWhen(predicate);
                }
            }

            // Trials
            foreach (var trial in experiment.Trials)
            {
                AddTrialToExperiment(exp, trial);
            }
        });
    }

    private void AddTrialToExperiment(ExperimentBuilder experimentBuilder, TrialConfig trial)
    {
        try
        {
            var serviceType = _typeResolver.Resolve(trial.ServiceType);

            // Get the Trial<TService> method and make it generic
            var trialMethod = typeof(ExperimentBuilder)
                .GetMethod(nameof(ExperimentBuilder.Trial))!
                .MakeGenericMethod(serviceType);

            // Create the configuration action
            var configureMethod = GetType()
                .GetMethod(nameof(CreateTrialConfigureAction), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(serviceType);

            var action = configureMethod.Invoke(this, [trial]);

            trialMethod.Invoke(experimentBuilder, [action]);
        }
        catch (Exception ex)
        {
            throw new ExperimentConfigurationException(
                $"Failed to add trial for service type '{trial.ServiceType}'", ex);
        }
    }
}
