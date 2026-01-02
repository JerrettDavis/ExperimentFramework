using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.ServiceRegistration.Validators;

/// <summary>
/// Base interface for all service registration validators.
/// </summary>
public interface IRegistrationValidator
{
    /// <summary>
    /// Validates a patch operation and returns any findings.
    /// </summary>
    /// <param name="operation">The operation to validate.</param>
    /// <param name="snapshot">The service graph snapshot before mutations.</param>
    /// <returns>A collection of validation findings.</returns>
    IEnumerable<ValidationFinding> Validate(ServiceGraphPatchOperation operation, ServiceGraphSnapshot snapshot);
}

/// <summary>
/// Validates that replacement implementations are assignable to the service type.
/// </summary>
public sealed class AssignabilityValidator : IRegistrationValidator
{
    /// <inheritdoc />
    public IEnumerable<ValidationFinding> Validate(ServiceGraphPatchOperation operation, ServiceGraphSnapshot snapshot)
    {
        foreach (var descriptor in operation.NewDescriptors)
        {
            // Check if implementation type is assignable to service type
            Type? implementationType = descriptor.ImplementationType;

            // If using factory, we can't validate at startup time
            if (descriptor.ImplementationFactory != null)
            {
                yield return ValidationFinding.Warning(
                    "Assignability",
                    operation.ServiceType,
                    $"Cannot validate assignability for factory-based registration. " +
                    $"Ensure factory returns an instance assignable to {operation.ServiceType.FullName}.",
                    "Validate factory return type manually or use implementation type registration.");
                continue;
            }

            if (descriptor.ImplementationInstance != null)
            {
                implementationType = descriptor.ImplementationInstance.GetType();
            }

            if (implementationType != null && !descriptor.ServiceType.IsAssignableFrom(implementationType))
            {
                yield return ValidationFinding.Error(
                    "Assignability",
                    operation.ServiceType,
                    $"Implementation type {implementationType.FullName} is not assignable to service type {descriptor.ServiceType.FullName}.",
                    $"Ensure {implementationType.FullName} implements or inherits from {descriptor.ServiceType.FullName}.");
            }
        }
    }
}

/// <summary>
/// Validates lifetime safety to prevent scoped services being injected into singletons.
/// </summary>
public sealed class LifetimeSafetyValidator : IRegistrationValidator
{
    /// <inheritdoc />
    public IEnumerable<ValidationFinding> Validate(ServiceGraphPatchOperation operation, ServiceGraphSnapshot snapshot)
    {
        foreach (var newDescriptor in operation.NewDescriptors)
        {
            // Find original descriptors for the same service type
            var originalDescriptors = snapshot.Descriptors
                .Where(d => d.ServiceType == operation.ServiceType)
                .ToList();

            var dangerousChanges = originalDescriptors
                .Where(o => IsDangerousLifetimeChange(o.Lifetime, newDescriptor.Lifetime))
                .ToList();

            foreach (var original in dangerousChanges)
            {
                // Check for lifetime violations
                yield return ValidationFinding.Error(
                    "LifetimeSafety",
                    operation.ServiceType,
                    $"Changing lifetime from {original.Lifetime} to {newDescriptor.Lifetime} may cause scoped service capture issues.",
                    $"Ensure the new lifetime ({newDescriptor.Lifetime}) is compatible with the original ({original.Lifetime}). " +
                    "Typically, you can change Singleton->Scoped->Transient but not the reverse.");
            }
        }
    }

    private static bool IsDangerousLifetimeChange(ServiceLifetime from, ServiceLifetime to)
    {
        // Singleton can't safely become Scoped or Transient (might capture scoped dependencies)
        // Singleton -> Scoped/Transient: Dangerous (scoped/transient captured in singleton)
        if (from == ServiceLifetime.Singleton && to != ServiceLifetime.Singleton)
            return true;

        return false;
    }
}

/// <summary>
/// Validates that open generic constraints are satisfied.
/// </summary>
public sealed class OpenGenericValidator : IRegistrationValidator
{
    /// <inheritdoc />
    public IEnumerable<ValidationFinding> Validate(ServiceGraphPatchOperation operation, ServiceGraphSnapshot snapshot)
    {
        if (!operation.ServiceType.IsGenericTypeDefinition)
        {
            yield break; // Not an open generic, nothing to validate
        }

        var descriptorValidations = operation.NewDescriptors
            .Select(descriptor => (descriptor, implementationType: descriptor.ImplementationType))
            .ToList();

        foreach (var (descriptor, implementationType) in descriptorValidations)
        {
            if (implementationType == null)
            {
                yield return ValidationFinding.Warning(
                    "OpenGeneric",
                    operation.ServiceType,
                    "Cannot validate open generic constraints for factory or instance-based registrations.",
                    "If using factories with open generics, ensure the factory correctly handles generic type constraints.");
                continue;
            }

            if (!implementationType.IsGenericTypeDefinition)
            {
                yield return ValidationFinding.Error(
                    "OpenGeneric",
                    operation.ServiceType,
                    $"Service type is an open generic ({operation.ServiceType.FullName}) but implementation type is not ({implementationType.FullName}).",
                    "The implementation type must also be an open generic with matching arity.");
                continue;
            }

            // Check generic arity
            var serviceArity = operation.ServiceType.GetGenericArguments().Length;
            var implArity = implementationType.GetGenericArguments().Length;

            if (serviceArity != implArity)
            {
                yield return ValidationFinding.Error(
                    "OpenGeneric",
                    operation.ServiceType,
                    $"Generic arity mismatch: service type has {serviceArity} type parameters but implementation has {implArity}.",
                    "Ensure the implementation type has the same number of generic type parameters as the service type.");
            }
        }
    }
}

/// <summary>
/// Validates that idempotency is maintained (no double-wrapping).
/// </summary>
public sealed class IdempotencyValidator : IRegistrationValidator
{
    /// <inheritdoc />
    public IEnumerable<ValidationFinding> Validate(ServiceGraphPatchOperation operation, ServiceGraphSnapshot snapshot)
    {
        // Check if we're trying to wrap an already-wrapped service
        // Look for implementation types that end with ExperimentProxy suffix or contain "Proxy" in namespace
        var existingProxies = snapshot.Descriptors
            .Where(d => d.ServiceType == operation.ServiceType &&
                       d.ImplementationType != null &&
                       (d.ImplementationType.Name.EndsWith("ExperimentProxy", StringComparison.Ordinal) ||
                        d.ImplementationType.Namespace?.Contains("ExperimentFramework") == true))
            .ToList();

        if (existingProxies.Any())
        {
            yield return ValidationFinding.Warning(
                "Idempotency",
                operation.ServiceType,
                $"Service type {operation.ServiceType.FullName} appears to already have an experiment proxy registered. " +
                "Applying this operation may result in double-wrapping.",
                "Ensure ExperimentFramework is only configured once for each service type.");
        }
    }
}

/// <summary>
/// Validates multi-registration ordering invariants.
/// </summary>
public sealed class MultiRegistrationValidator : IRegistrationValidator
{
    /// <inheritdoc />
    public IEnumerable<ValidationFinding> Validate(ServiceGraphPatchOperation operation, ServiceGraphSnapshot snapshot)
    {
        var existingRegistrations = snapshot.Descriptors
            .Where(d => d.ServiceType == operation.ServiceType)
            .ToList();

        if (existingRegistrations.Count <= 1)
        {
            yield break; // Not a multi-registration scenario
        }

        // Warn about multi-registration operations
        if (operation.OperationType == MultiRegistrationBehavior.Replace && existingRegistrations.Count > 1)
        {
            yield return ValidationFinding.Warning(
                "MultiRegistration",
                operation.ServiceType,
                $"Replace operation will remove {existingRegistrations.Count} existing registrations for {operation.ServiceType.FullName}. " +
                "This may affect IEnumerable<T> resolution.",
                "Consider using Merge operation to preserve multi-registration semantics, or use Insert/Append to maintain ordering.");
        }
    }
}
