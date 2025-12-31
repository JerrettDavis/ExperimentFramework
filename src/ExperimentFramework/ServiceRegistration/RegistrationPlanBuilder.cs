using ExperimentFramework.Models;
using ExperimentFramework.ServiceRegistration.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.ServiceRegistration;

/// <summary>
/// Builds registration plans from experiment definitions.
/// </summary>
public sealed class RegistrationPlanBuilder
{
    private readonly List<ServiceGraphPatchOperation> _operations = new();
    private readonly List<IRegistrationValidator> _validators = new();
    private ValidationMode _validationMode = ValidationMode.Strict;
    private MultiRegistrationBehavior _defaultBehavior = MultiRegistrationBehavior.Replace;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegistrationPlanBuilder"/> class.
    /// </summary>
    public RegistrationPlanBuilder()
    {
        // Register default validators
        _validators.Add(new AssignabilityValidator());
        _validators.Add(new LifetimeSafetyValidator());
        _validators.Add(new OpenGenericValidator());
        _validators.Add(new IdempotencyValidator());
        _validators.Add(new MultiRegistrationValidator());
    }

    /// <summary>
    /// Sets the validation mode for the plan.
    /// </summary>
    public RegistrationPlanBuilder WithValidationMode(ValidationMode mode)
    {
        _validationMode = mode;
        return this;
    }

    /// <summary>
    /// Sets the default multi-registration behavior.
    /// </summary>
    public RegistrationPlanBuilder WithDefaultBehavior(MultiRegistrationBehavior behavior)
    {
        _defaultBehavior = behavior;
        return this;
    }

    /// <summary>
    /// Adds a custom validator to the validation pipeline.
    /// </summary>
    public RegistrationPlanBuilder AddValidator(IRegistrationValidator validator)
    {
        _validators.Add(validator);
        return this;
    }

    /// <summary>
    /// Adds an operation to the plan.
    /// </summary>
    public RegistrationPlanBuilder AddOperation(ServiceGraphPatchOperation operation)
    {
        _operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Builds a plan from experiment definitions.
    /// </summary>
    /// <param name="snapshot">The service graph snapshot.</param>
    /// <param name="definitions">The experiment definitions.</param>
    /// <param name="config">The experiment framework configuration.</param>
    /// <returns>A registration plan ready for validation and execution.</returns>
    internal RegistrationPlan BuildFromDefinitions(
        ServiceGraphSnapshot snapshot,
        IExperimentDefinition[] definitions,
        ExperimentFrameworkConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(config);

        // Create operations from definitions
        foreach (var definition in definitions)
        {
            var operation = CreateOperationFromDefinition(definition, snapshot, config);
            _operations.Add(operation);
        }

        return Build(snapshot);
    }

    /// <summary>
    /// Builds and validates the registration plan.
    /// </summary>
    /// <param name="snapshot">The service graph snapshot.</param>
    /// <returns>A validated registration plan.</returns>
    public RegistrationPlan Build(ServiceGraphSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var planId = Guid.NewGuid().ToString("N");
        var findings = new List<ValidationFinding>();

        // Validate each operation if validation is enabled
        if (_validationMode != ValidationMode.Off)
        {
            foreach (var operation in _operations)
            {
                foreach (var validator in _validators)
                {
                    findings.AddRange(validator.Validate(operation, snapshot));
                }
            }
        }

        // Determine if plan is valid based on validation mode
        bool isValid = _validationMode switch
        {
            ValidationMode.Off => true,
            ValidationMode.Warn => true, // Warnings don't block execution
            ValidationMode.Strict => !findings.Any(f => f.Severity == ValidationSeverity.Error),
            _ => false
        };

        return new RegistrationPlan(
            planId,
            snapshot,
            _operations.AsReadOnly(),
            findings.AsReadOnly(),
            isValid,
            _validationMode);
    }

    /// <summary>
    /// Creates a patch operation from an experiment definition.
    /// </summary>
    private ServiceGraphPatchOperation CreateOperationFromDefinition(
        IExperimentDefinition definition,
        ServiceGraphSnapshot snapshot,
        ExperimentFrameworkConfiguration config)
    {
        var serviceType = definition.ServiceType;
        var operationId = Guid.NewGuid().ToString("N");

        // Find the proxy descriptor that will be created by the framework
        // For now, we'll create a placeholder that matches the existing behavior
        var existingDescriptor = snapshot.Descriptors.FirstOrDefault(d => d.ServiceType == serviceType);

        if (existingDescriptor == null)
        {
            throw new InvalidOperationException(
                $"Service type {serviceType.FullName} is not registered in the service collection. " +
                "Ensure the service is registered before configuring experiments.");
        }

        // Create a placeholder descriptor for the proxy
        // In the actual implementation, this would be the proxy factory
        var proxyDescriptor = new ServiceDescriptor(
            serviceType,
            sp => throw new NotImplementedException("Proxy factory placeholder"),
            ServiceLifetime.Singleton); // Proxies are always singletons

        // Match predicate: find descriptors for this service type
        Func<ServiceDescriptor, bool> matchPredicate = d => d.ServiceType == serviceType;

        var metadata = new OperationMetadata(
            $"Replace {serviceType.Name} registration with experiment proxy",
            new Dictionary<string, string>
            {
                ["ExperimentName"] = definition.ServiceType.Name,
                ["OriginalLifetime"] = existingDescriptor.Lifetime.ToString()
            });

        return new ServiceGraphPatchOperation(
            operationId,
            _defaultBehavior,
            serviceType,
            matchPredicate,
            new[] { proxyDescriptor },
            expectedMatchCount: 1,
            allowNoMatches: false,
            metadata);
    }
}
