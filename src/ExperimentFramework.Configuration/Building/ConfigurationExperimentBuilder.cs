using System.Reflection;
using ExperimentFramework.Configuration.Activation;
using ExperimentFramework.Configuration.Exceptions;
using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Models;
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
    private readonly ILogger<ConfigurationExperimentBuilder>? _logger;

    /// <summary>
    /// Creates a new configuration experiment builder.
    /// </summary>
    public ConfigurationExperimentBuilder(ITypeResolver typeResolver, ILogger<ConfigurationExperimentBuilder>? logger = null)
    {
        _typeResolver = typeResolver;
        _logger = logger;
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
        switch (decorator.Type.ToLowerInvariant())
        {
            case "logging":
                AddLoggingDecorator(builder, decorator);
                break;

            case "timeout":
                AddTimeoutDecorator(builder, decorator);
                break;

            case "circuitbreaker":
                AddCircuitBreakerDecorator(builder, decorator);
                break;

            case "outcomecollection":
                AddOutcomeCollectionDecorator(builder, decorator);
                break;

            case "custom":
                AddCustomDecorator(builder, decorator);
                break;

            default:
                _logger?.LogWarning("Unknown decorator type '{Type}', skipping", decorator.Type);
                break;
        }
    }

    private void AddLoggingDecorator(ExperimentFrameworkBuilder builder, DecoratorConfig decorator)
    {
        builder.AddLogger(l =>
        {
            if (decorator.Options != null)
            {
                if (GetBoolOption(decorator.Options, "benchmarks"))
                {
                    l.AddBenchmarks();
                }
                if (GetBoolOption(decorator.Options, "errorLogging"))
                {
                    l.AddErrorLogging();
                }
            }
        });
    }

    private void AddTimeoutDecorator(ExperimentFrameworkBuilder builder, DecoratorConfig decorator)
    {
        var timeout = TimeSpan.FromSeconds(30);
        var onTimeout = TimeoutAction.FallbackToDefault;
        string? fallbackKey = null;

        if (decorator.Options != null)
        {
            if (TryGetTimeSpanOption(decorator.Options, "timeout", out var t))
            {
                timeout = t;
            }

            if (decorator.Options.TryGetValue("onTimeout", out var action) && action is string actionStr)
            {
                onTimeout = actionStr.ToLowerInvariant() switch
                {
                    "throw" or "throwexception" => TimeoutAction.ThrowException,
                    "fallbacktodefault" => TimeoutAction.FallbackToDefault,
                    "fallbacktospecifictrial" => TimeoutAction.FallbackToSpecificTrial,
                    _ => TimeoutAction.FallbackToDefault
                };
            }

            if (decorator.Options.TryGetValue("fallbackTrialKey", out var key) && key is string keyStr)
            {
                fallbackKey = keyStr;
            }
        }

        builder.WithTimeout(timeout, onTimeout, fallbackKey);
    }

    private void AddCircuitBreakerDecorator(ExperimentFrameworkBuilder builder, DecoratorConfig decorator)
    {
        // Try to find and invoke WithCircuitBreaker via reflection
        // This allows the Configuration package to work without a direct reference to Resilience
        var resilienceExtensionsType = Type.GetType(
            "ExperimentFramework.Resilience.ResilienceBuilderExtensions, ExperimentFramework.Resilience");

        if (resilienceExtensionsType == null)
        {
            _logger?.LogWarning(
                "Circuit breaker decorator requires ExperimentFramework.Resilience package. Skipping.");
            return;
        }

        try
        {
            // Find CircuitBreakerOptions type
            var optionsType = Type.GetType(
                "ExperimentFramework.Resilience.CircuitBreakerOptions, ExperimentFramework.Resilience");

            if (optionsType == null)
            {
                _logger?.LogWarning("Could not find CircuitBreakerOptions type. Skipping circuit breaker.");
                return;
            }

            // Create and configure options
            var options = Activator.CreateInstance(optionsType);
            if (options != null && decorator.Options != null)
            {
                ConfigureCircuitBreakerOptions(options, optionsType, decorator.Options);
            }

            // Find and invoke WithCircuitBreaker method
            var method = resilienceExtensionsType.GetMethod("WithCircuitBreaker",
                [typeof(ExperimentFrameworkBuilder), optionsType, typeof(ILoggerFactory)]);

            method?.Invoke(null, [builder, options, null]);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to configure circuit breaker decorator");
        }
    }

    private void ConfigureCircuitBreakerOptions(object options, Type optionsType, Dictionary<string, object> config)
    {
        foreach (var (key, value) in config)
        {
            var property = optionsType.GetProperty(key,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property == null || !property.CanWrite)
                continue;

            try
            {
                object convertedValue = value;

                if (property.PropertyType == typeof(TimeSpan) && value is string timeStr)
                {
                    convertedValue = TimeSpan.Parse(timeStr);
                }
                else if (property.PropertyType == typeof(int) && value is not int)
                {
                    convertedValue = Convert.ToInt32(value);
                }
                else if (property.PropertyType == typeof(double) && value is not double)
                {
                    convertedValue = Convert.ToDouble(value);
                }
                else if (property.PropertyType == typeof(double?) && value != null)
                {
                    convertedValue = Convert.ToDouble(value);
                }

                property.SetValue(options, convertedValue);
            }
            catch
            {
                // Ignore property setting errors
            }
        }
    }

    private void AddOutcomeCollectionDecorator(ExperimentFrameworkBuilder builder, DecoratorConfig decorator)
    {
        // Try to find and invoke WithOutcomeCollection via reflection
        var dataExtensionsType = Type.GetType(
            "ExperimentFramework.Data.ExperimentBuilderExtensions, ExperimentFramework.Data");

        if (dataExtensionsType == null)
        {
            _logger?.LogWarning(
                "Outcome collection decorator requires ExperimentFramework.Data package. Skipping.");
            return;
        }

        try
        {
            // Find the method that takes Action<OutcomeRecorderOptions>
            var methods = dataExtensionsType.GetMethods()
                .Where(m => m.Name == "WithOutcomeCollection")
                .ToList();

            var method = methods.FirstOrDefault(m =>
            {
                var parameters = m.GetParameters();
                return parameters.Length == 2 &&
                       parameters[0].ParameterType == typeof(ExperimentFrameworkBuilder);
            });

            if (method != null)
            {
                // Just call with null action for now - options configuration would require more complex reflection
                method.Invoke(null, [builder, null]);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to configure outcome collection decorator");
        }
    }

    private void AddCustomDecorator(ExperimentFrameworkBuilder builder, DecoratorConfig decorator)
    {
        if (string.IsNullOrEmpty(decorator.TypeName))
        {
            _logger?.LogWarning("Custom decorator missing typeName, skipping");
            return;
        }

        try
        {
            var factoryType = _typeResolver.Resolve(decorator.TypeName);
            if (Activator.CreateInstance(factoryType) is Decorators.IExperimentDecoratorFactory factory)
            {
                builder.AddDecoratorFactory(factory);
            }
            else
            {
                _logger?.LogWarning("Type '{Type}' does not implement IExperimentDecoratorFactory",
                    decorator.TypeName);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to create custom decorator '{Type}'", decorator.TypeName);
        }
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
        switch (mode.Type.ToLowerInvariant())
        {
            case "featureflag":
                builder.UsingFeatureFlag(mode.FlagName);
                break;

            case "configurationkey":
                builder.UsingConfigurationKey(mode.Key);
                break;

            case "variantfeatureflag":
                builder.UsingCustomMode("VariantFeatureFlag", mode.FlagName);
                break;

            case "openfeature":
                builder.UsingCustomMode("OpenFeature", mode.FlagKey);
                break;

            case "stickyrouting":
                builder.UsingCustomMode("StickyRouting", mode.SelectorName);
                break;

            case "custom":
                builder.UsingCustomMode(mode.ModeIdentifier!, mode.SelectorName);
                break;
        }
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
