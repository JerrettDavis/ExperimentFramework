using ExperimentFramework.Configuration.Extensions;
using ExperimentFramework.Configuration.Models;

namespace ExperimentFramework.Configuration.Validation;

/// <summary>
/// Default implementation of configuration validation.
/// </summary>
public sealed class ConfigurationValidator : IConfigurationValidator
{
    private readonly ConfigurationExtensionRegistry? _extensionRegistry;

    // Built-in types that are always valid (for backward compatibility when no registry)
    // Note: Some of these require additional packages to actually work at runtime
    private static readonly HashSet<string> BuiltInSelectionModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "featureFlag", "configurationKey", "custom",
        // Extension package modes (require package to be installed for runtime)
        "variantFeatureFlag", "openFeature", "stickyRouting",
        "rollout", "stagedRollout", "targeting"
    };

    private static readonly HashSet<string> BuiltInDecoratorTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "logging", "timeout", "metrics", "killSwitch", "custom",
        // Extension package decorators (require package to be installed for runtime)
        "circuitBreaker", "outcomeCollection"
    };

    private static readonly HashSet<string> ValidErrorPolicies = new(StringComparer.OrdinalIgnoreCase)
    {
        "throw", "fallbackToControl", "fallbackTo", "tryInOrder", "tryAny"
    };

    private static readonly HashSet<string> ValidHypothesisTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "superiority", "nonInferiority", "equivalence", "twoSided"
    };

    private static readonly HashSet<string> ValidOutcomeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "binary", "continuous", "count", "duration"
    };

    /// <summary>
    /// Creates a new validator without an extension registry (backward compatible).
    /// </summary>
    public ConfigurationValidator() : this(null)
    {
    }

    /// <summary>
    /// Creates a new validator with an optional extension registry.
    /// </summary>
    /// <param name="extensionRegistry">Optional registry for custom decorator and selection mode validation.</param>
    public ConfigurationValidator(ConfigurationExtensionRegistry? extensionRegistry)
    {
        _extensionRegistry = extensionRegistry;
    }

    /// <inheritdoc />
    public ConfigurationValidationResult Validate(ExperimentFrameworkConfigurationRoot config)
    {
        var errors = new List<ConfigurationValidationError>();

        // Validate decorators
        if (config.Decorators != null)
        {
            for (var i = 0; i < config.Decorators.Count; i++)
            {
                ValidateDecorator(config.Decorators[i], $"decorators[{i}]", errors);
            }
        }

        // Validate standalone trials
        if (config.Trials != null)
        {
            for (var i = 0; i < config.Trials.Count; i++)
            {
                ValidateTrial(config.Trials[i], $"trials[{i}]", errors);
            }
        }

        // Validate named experiments
        if (config.Experiments != null)
        {
            var experimentNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < config.Experiments.Count; i++)
            {
                var experiment = config.Experiments[i];
                ValidateExperiment(experiment, $"experiments[{i}]", errors);

                // Check for duplicate experiment names
                if (!experimentNames.Add(experiment.Name))
                {
                    errors.Add(ConfigurationValidationError.Error(
                        $"experiments[{i}].name",
                        $"Duplicate experiment name: '{experiment.Name}'"));
                }
            }
        }

        return new ConfigurationValidationResult(errors);
    }

    private void ValidateDecorator(DecoratorConfig decorator, string path, List<ConfigurationValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(decorator.Type))
        {
            errors.Add(ConfigurationValidationError.Error($"{path}.type", "Decorator type is required"));
            return;
        }

        // Check if type is valid - either registered in the extension registry or a built-in type
        var isValidType = _extensionRegistry?.HasDecoratorHandler(decorator.Type) == true
                          || BuiltInDecoratorTypes.Contains(decorator.Type);

        if (!isValidType)
        {
            // Get valid types for the error message
            var validTypes = GetValidDecoratorTypes();
            errors.Add(ConfigurationValidationError.Error(
                $"{path}.type",
                $"Invalid decorator type: '{decorator.Type}'. Valid values: {string.Join(", ", validTypes)}"));
            return;
        }

        // Use handler-specific validation if available
        var handler = _extensionRegistry?.GetDecoratorHandler(decorator.Type);
        if (handler != null)
        {
            errors.AddRange(handler.Validate(decorator, path));
        }
        else if (decorator.Type.Equals("custom", StringComparison.OrdinalIgnoreCase) &&
                 string.IsNullOrWhiteSpace(decorator.TypeName))
        {
            errors.Add(ConfigurationValidationError.Error(
                $"{path}.typeName",
                "Type name is required for custom decorators"));
        }
    }

    private IEnumerable<string> GetValidDecoratorTypes()
    {
        var types = new HashSet<string>(BuiltInDecoratorTypes, StringComparer.OrdinalIgnoreCase);
        if (_extensionRegistry != null)
        {
            foreach (var type in _extensionRegistry.GetRegisteredDecoratorTypes())
            {
                types.Add(type);
            }
        }
        return types.OrderBy(t => t);
    }

    private void ValidateTrial(TrialConfig trial, string path, List<ConfigurationValidationError> errors)
    {
        // Required fields
        if (string.IsNullOrWhiteSpace(trial.ServiceType))
        {
            errors.Add(ConfigurationValidationError.Error($"{path}.serviceType", "Service type is required"));
        }

        // Selection mode validation
        if (trial.SelectionMode == null)
        {
            errors.Add(ConfigurationValidationError.Error($"{path}.selectionMode", "Selection mode is required"));
        }
        else
        {
            ValidateSelectionMode(trial.SelectionMode, $"{path}.selectionMode", errors);
        }

        // Control validation
        if (trial.Control == null)
        {
            errors.Add(ConfigurationValidationError.Error($"{path}.control", "Control implementation is required"));
        }
        else
        {
            ValidateCondition(trial.Control, $"{path}.control", errors);
        }

        // Conditions validation
        if (trial.Conditions != null)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (trial.Control != null)
            {
                keys.Add(trial.Control.Key);
            }

            for (var i = 0; i < trial.Conditions.Count; i++)
            {
                var condition = trial.Conditions[i];
                ValidateCondition(condition, $"{path}.conditions[{i}]", errors);

                if (!keys.Add(condition.Key))
                {
                    errors.Add(ConfigurationValidationError.Error(
                        $"{path}.conditions[{i}].key",
                        $"Duplicate condition key: '{condition.Key}'"));
                }
            }
        }

        // Error policy validation
        if (trial.ErrorPolicy != null)
        {
            ValidateErrorPolicy(trial.ErrorPolicy, trial, $"{path}.errorPolicy", errors);
        }

        // Activation validation
        if (trial.Activation != null)
        {
            ValidateActivation(trial.Activation, $"{path}.activation", errors);
        }
    }

    private void ValidateSelectionMode(SelectionModeConfig mode, string path, List<ConfigurationValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(mode.Type))
        {
            errors.Add(ConfigurationValidationError.Error($"{path}.type", "Selection mode type is required"));
            return;
        }

        // Check if type is valid - either registered in the extension registry or a built-in type
        var isValidType = _extensionRegistry?.HasSelectionModeHandler(mode.Type) == true
                          || BuiltInSelectionModes.Contains(mode.Type);

        if (!isValidType)
        {
            var validTypes = GetValidSelectionModeTypes();
            errors.Add(ConfigurationValidationError.Error(
                $"{path}.type",
                $"Invalid selection mode type: '{mode.Type}'. Valid values: {string.Join(", ", validTypes)}"));
            return;
        }

        // Use handler-specific validation if available
        var handler = _extensionRegistry?.GetSelectionModeHandler(mode.Type);
        if (handler != null)
        {
            errors.AddRange(handler.Validate(mode, path));
        }
        else if (mode.Type.Equals("custom", StringComparison.OrdinalIgnoreCase) &&
                 string.IsNullOrWhiteSpace(mode.ModeIdentifier))
        {
            errors.Add(ConfigurationValidationError.Error(
                $"{path}.modeIdentifier",
                "Mode identifier is required for custom selection mode"));
        }
    }

    private IEnumerable<string> GetValidSelectionModeTypes()
    {
        var types = new HashSet<string>(BuiltInSelectionModes, StringComparer.OrdinalIgnoreCase);
        if (_extensionRegistry != null)
        {
            foreach (var type in _extensionRegistry.GetRegisteredSelectionModeTypes())
            {
                types.Add(type);
            }
        }
        return types.OrderBy(t => t);
    }

    private static void ValidateCondition(ConditionConfig condition, string path, List<ConfigurationValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(condition.Key))
        {
            errors.Add(ConfigurationValidationError.Error($"{path}.key", "Condition key is required"));
        }

        if (string.IsNullOrWhiteSpace(condition.ImplementationType))
        {
            errors.Add(ConfigurationValidationError.Error($"{path}.implementationType", "Implementation type is required"));
        }
    }

    private static void ValidateErrorPolicy(ErrorPolicyConfig policy, TrialConfig trial, string path, List<ConfigurationValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(policy.Type))
        {
            errors.Add(ConfigurationValidationError.Error($"{path}.type", "Error policy type is required"));
            return;
        }

        if (!ValidErrorPolicies.Contains(policy.Type))
        {
            errors.Add(ConfigurationValidationError.Error(
                $"{path}.type",
                $"Invalid error policy type: '{policy.Type}'. Valid values: {string.Join(", ", ValidErrorPolicies)}"));
            return;
        }

        // Collect all valid condition keys
        var allKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (trial.Control != null)
        {
            allKeys.Add(trial.Control.Key);
        }
        if (trial.Conditions != null)
        {
            foreach (var c in trial.Conditions)
            {
                allKeys.Add(c.Key);
            }
        }

        // Validate fallback references
        if (policy.Type.Equals("fallbackTo", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(policy.FallbackKey))
            {
                errors.Add(ConfigurationValidationError.Error(
                    $"{path}.fallbackKey",
                    "Fallback key is required for 'fallbackTo' policy"));
            }
            else if (!allKeys.Contains(policy.FallbackKey))
            {
                errors.Add(ConfigurationValidationError.Warning(
                    $"{path}.fallbackKey",
                    $"Fallback key '{policy.FallbackKey}' does not match any defined condition"));
            }
        }

        if (policy.Type.Equals("tryInOrder", StringComparison.OrdinalIgnoreCase))
        {
            if (policy.FallbackKeys == null || policy.FallbackKeys.Count == 0)
            {
                errors.Add(ConfigurationValidationError.Error(
                    $"{path}.fallbackKeys",
                    "Fallback keys list is required for 'tryInOrder' policy"));
            }
            else
            {
                for (var i = 0; i < policy.FallbackKeys.Count; i++)
                {
                    var key = policy.FallbackKeys[i];
                    if (!allKeys.Contains(key))
                    {
                        errors.Add(ConfigurationValidationError.Warning(
                            $"{path}.fallbackKeys[{i}]",
                            $"Fallback key '{key}' does not match any defined condition"));
                    }
                }
            }
        }
    }

    private static void ValidateActivation(ActivationConfig activation, string path, List<ConfigurationValidationError> errors)
    {
        if (activation is { From: not null, Until: not null })
        {
            if (activation.From >= activation.Until)
            {
                errors.Add(ConfigurationValidationError.Error(
                    path,
                    "Activation 'from' must be before 'until'"));
            }
        }

        if (activation.Predicate != null && string.IsNullOrWhiteSpace(activation.Predicate.Type))
        {
            errors.Add(ConfigurationValidationError.Error(
                $"{path}.predicate.type",
                "Predicate type is required"));
        }
    }

    private void ValidateExperiment(ExperimentConfig experiment, string path, List<ConfigurationValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(experiment.Name))
        {
            errors.Add(ConfigurationValidationError.Error($"{path}.name", "Experiment name is required"));
        }

        if (experiment.Trials == null || experiment.Trials.Count == 0)
        {
            errors.Add(ConfigurationValidationError.Error($"{path}.trials", "Experiment must have at least one trial"));
        }
        else
        {
            for (var i = 0; i < experiment.Trials.Count; i++)
            {
                ValidateTrial(experiment.Trials[i], $"{path}.trials[{i}]", errors);
            }
        }

        if (experiment.Activation != null)
        {
            ValidateActivation(experiment.Activation, $"{path}.activation", errors);
        }

        if (experiment.Hypothesis != null)
        {
            ValidateHypothesis(experiment.Hypothesis, $"{path}.hypothesis", errors);
        }
    }

    private static void ValidateHypothesis(HypothesisConfig hypothesis, string path, List<ConfigurationValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(hypothesis.Name))
        {
            errors.Add(ConfigurationValidationError.Error($"{path}.name", "Hypothesis name is required"));
        }

        if (string.IsNullOrWhiteSpace(hypothesis.Type))
        {
            errors.Add(ConfigurationValidationError.Error($"{path}.type", "Hypothesis type is required"));
        }
        else if (!ValidHypothesisTypes.Contains(hypothesis.Type))
        {
            errors.Add(ConfigurationValidationError.Error(
                $"{path}.type",
                $"Invalid hypothesis type: '{hypothesis.Type}'. Valid values: {string.Join(", ", ValidHypothesisTypes)}"));
        }

        if (string.IsNullOrWhiteSpace(hypothesis.NullHypothesis))
        {
            errors.Add(ConfigurationValidationError.Error($"{path}.nullHypothesis", "Null hypothesis is required"));
        }

        if (string.IsNullOrWhiteSpace(hypothesis.AlternativeHypothesis))
        {
            errors.Add(ConfigurationValidationError.Error($"{path}.alternativeHypothesis", "Alternative hypothesis is required"));
        }

        if (hypothesis.PrimaryEndpoint == null)
        {
            errors.Add(ConfigurationValidationError.Error($"{path}.primaryEndpoint", "Primary endpoint is required"));
        }
        else
        {
            ValidateEndpoint(hypothesis.PrimaryEndpoint, $"{path}.primaryEndpoint", errors);
        }

        if (hypothesis.SecondaryEndpoints != null)
        {
            for (var i = 0; i < hypothesis.SecondaryEndpoints.Count; i++)
            {
                ValidateEndpoint(hypothesis.SecondaryEndpoints[i], $"{path}.secondaryEndpoints[{i}]", errors);
            }
        }

        if (hypothesis.ExpectedEffectSize <= 0)
        {
            errors.Add(ConfigurationValidationError.Error(
                $"{path}.expectedEffectSize",
                "Expected effect size must be positive"));
        }

        if (hypothesis.SuccessCriteria == null)
        {
            errors.Add(ConfigurationValidationError.Error($"{path}.successCriteria", "Success criteria is required"));
        }
        else
        {
            ValidateSuccessCriteria(hypothesis.SuccessCriteria, $"{path}.successCriteria", errors);
        }
    }

    private static void ValidateEndpoint(EndpointConfig endpoint, string path, List<ConfigurationValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(endpoint.Name))
        {
            errors.Add(ConfigurationValidationError.Error($"{path}.name", "Endpoint name is required"));
        }

        if (string.IsNullOrWhiteSpace(endpoint.OutcomeType))
        {
            errors.Add(ConfigurationValidationError.Error($"{path}.outcomeType", "Outcome type is required"));
        }
        else if (!ValidOutcomeTypes.Contains(endpoint.OutcomeType))
        {
            errors.Add(ConfigurationValidationError.Error(
                $"{path}.outcomeType",
                $"Invalid outcome type: '{endpoint.OutcomeType}'. Valid values: {string.Join(", ", ValidOutcomeTypes)}"));
        }

        if (endpoint is { HigherIsBetter: true, LowerIsBetter: true })
        {
            errors.Add(ConfigurationValidationError.Error(
                path,
                "Cannot specify both 'higherIsBetter' and 'lowerIsBetter' as true"));
        }
    }

    private static void ValidateSuccessCriteria(SuccessCriteriaConfig criteria, string path, List<ConfigurationValidationError> errors)
    {
        if (criteria.Alpha is <= 0 or >= 1)
        {
            errors.Add(ConfigurationValidationError.Error(
                $"{path}.alpha",
                "Alpha must be between 0 and 1 (exclusive)"));
        }

        if (criteria.Power is <= 0 or >= 1)
        {
            errors.Add(ConfigurationValidationError.Error(
                $"{path}.power",
                "Power must be between 0 and 1 (exclusive)"));
        }

        if (criteria.MinimumSampleSize is <= 0)
        {
            errors.Add(ConfigurationValidationError.Error(
                $"{path}.minimumSampleSize",
                "Minimum sample size must be positive"));
        }
    }
}
