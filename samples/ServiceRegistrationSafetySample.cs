using ExperimentFramework.ServiceRegistration;
using ExperimentFramework.ServiceRegistration.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Samples.ServiceRegistrationSafety;

/// <summary>
/// Comprehensive sample demonstrating all service registration safety features.
/// </summary>
public class ServiceRegistrationSafetySample
{
    /// <summary>
    /// Demonstrates basic snapshot capture and plan creation.
    /// </summary>
    public static void BasicSnapshotAndPlanExample()
    {
        Console.WriteLine("=== Basic Snapshot and Plan Example ===\n");

        // 1. Create a service collection with some registrations
        var services = new ServiceCollection();
        services.AddSingleton<IDatabaseService, SqlDatabaseService>();
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddTransient<IEmailService, SmtpEmailService>();

        Console.WriteLine($"Original service count: {services.Count}");

        // 2. Capture a snapshot before mutations
        var snapshot = ServiceGraphSnapshot.Capture(services);
        Console.WriteLine($"\nSnapshot captured:");
        Console.WriteLine($"  ID: {snapshot.SnapshotId}");
        Console.WriteLine($"  Timestamp: {snapshot.Timestamp:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"  Descriptor count: {snapshot.Descriptors.Count}");
        Console.WriteLine($"  Fingerprint: {snapshot.Fingerprint}");

        // 3. Create a simple registration plan
        var planBuilder = new RegistrationPlanBuilder()
            .WithValidationMode(ValidationMode.Strict)
            .WithDefaultBehavior(MultiRegistrationBehavior.Replace);

        var plan = planBuilder.Build(snapshot);

        Console.WriteLine($"\nPlan created:");
        Console.WriteLine($"  Plan ID: {plan.PlanId}");
        Console.WriteLine($"  Valid: {plan.IsValid}");
        Console.WriteLine($"  Operations: {plan.Operations.Count}");
        Console.WriteLine($"  Findings: {plan.Findings.Count}");
    }

    /// <summary>
    /// Demonstrates different validation modes.
    /// </summary>
    public static void ValidationModesExample()
    {
        Console.WriteLine("\n=== Validation Modes Example ===\n");

        var services = new ServiceCollection();
        services.AddSingleton<IDatabaseService, SqlDatabaseService>();

        var snapshot = ServiceGraphSnapshot.Capture(services);

        // Try different validation modes
        var modes = new[] { ValidationMode.Off, ValidationMode.Warn, ValidationMode.Strict };

        foreach (var mode in modes)
        {
            var planBuilder = new RegistrationPlanBuilder()
                .WithValidationMode(mode);

            var plan = planBuilder.Build(snapshot);

            Console.WriteLine($"Validation Mode: {mode}");
            Console.WriteLine($"  Plan valid: {plan.IsValid}");
            Console.WriteLine($"  Error count: {plan.ErrorCount}");
            Console.WriteLine($"  Warning count: {plan.WarningCount}");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Demonstrates multi-registration behaviors.
    /// </summary>
    public static void MultiRegistrationBehaviorsExample()
    {
        Console.WriteLine("=== Multi-Registration Behaviors Example ===\n");

        var services = new ServiceCollection();
        
        // Register multiple implementations of the same service
        services.AddSingleton<INotificationService, EmailNotificationService>();
        services.AddSingleton<INotificationService, SmsNotificationService>();
        services.AddSingleton<INotificationService, PushNotificationService>();

        Console.WriteLine($"Original registrations: {services.Count(d => d.ServiceType == typeof(INotificationService))}");

        // Capture snapshot for later operations
        _ = ServiceGraphSnapshot.Capture(services);

        // Try different behaviors
        var behaviors = new[]
        {
            MultiRegistrationBehavior.Replace,
            MultiRegistrationBehavior.Insert,
            MultiRegistrationBehavior.Append,
            MultiRegistrationBehavior.Merge
        };

        foreach (var behavior in behaviors)
        {
            var testServices = new ServiceCollection();
            foreach (var descriptor in services)
            {
                testServices.Add(descriptor);
            }

            var operation = new ServiceGraphPatchOperation(
                operationId: Guid.NewGuid().ToString("N"),
                operationType: behavior,
                serviceType: typeof(INotificationService),
                matchPredicate: d => d.ServiceType == typeof(INotificationService),
                newDescriptors: new[]
                {
                    ServiceDescriptor.Singleton<INotificationService, ExperimentNotificationProxy>()
                },
                expectedMatchCount: null,
                allowNoMatches: false
            );

            var result = operation.Execute(testServices);

            Console.WriteLine($"Behavior: {behavior}");
            Console.WriteLine($"  Success: {result.Success}");
            Console.WriteLine($"  Matched: {result.MatchCount}");
            Console.WriteLine($"  Removed: {result.RemovedDescriptors.Count}");
            Console.WriteLine($"  Added: {result.AddedDescriptors.Count}");
            Console.WriteLine($"  Final count: {testServices.Count(d => d.ServiceType == typeof(INotificationService))}");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Demonstrates validators and validation findings.
    /// </summary>
    public static void ValidatorsExample()
    {
        Console.WriteLine("=== Validators Example ===\n");

        var services = new ServiceCollection();
        services.AddSingleton<IDatabaseService, SqlDatabaseService>();
        services.AddScoped<ICacheService, RedisCacheService>();

        var snapshot = ServiceGraphSnapshot.Capture(services);

        // Create operations that will trigger different validators
        var operations = new List<ServiceGraphPatchOperation>();

        // This will trigger lifetime safety validator (singleton -> scoped is dangerous)
        var lifetimeOperation = new ServiceGraphPatchOperation(
            operationId: "lifetime-test",
            operationType: MultiRegistrationBehavior.Replace,
            serviceType: typeof(ICacheService),
            matchPredicate: d => d.ServiceType == typeof(ICacheService),
            newDescriptors: new[]
            {
                ServiceDescriptor.Transient<ICacheService, MemoryCacheService>()
            }
        );
        operations.Add(lifetimeOperation);

        // Run validators
        var validators = new IRegistrationValidator[]
        {
            new AssignabilityValidator(),
            new LifetimeSafetyValidator(),
            new OpenGenericValidator(),
            new IdempotencyValidator(),
            new MultiRegistrationValidator()
        };

        foreach (var operation in operations)
        {
            Console.WriteLine($"Operation: {operation.OperationId}");
            
            foreach (var validator in validators)
            {
                var findings = validator.Validate(operation, snapshot).ToList();
                
                if (findings.Any())
                {
                    Console.WriteLine($"  {validator.GetType().Name}:");
                    foreach (var finding in findings)
                    {
                        Console.WriteLine($"    [{finding.Severity}] {finding.RuleName}");
                        Console.WriteLine($"      {finding.Description}");
                        if (!string.IsNullOrEmpty(finding.RecommendedAction))
                        {
                            Console.WriteLine($"      → {finding.RecommendedAction}");
                        }
                    }
                }
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Demonstrates plan execution with rollback.
    /// </summary>
    public static void PlanExecutionExample()
    {
        Console.WriteLine("=== Plan Execution Example ===\n");

        var services = new ServiceCollection();
        services.AddSingleton<IDatabaseService, SqlDatabaseService>();
        services.AddScoped<ICacheService, RedisCacheService>();

        Console.WriteLine($"Initial service count: {services.Count}");

        var snapshot = ServiceGraphSnapshot.Capture(services);

        // Create a valid operation
        var operation = new ServiceGraphPatchOperation(
            operationId: "replace-db",
            operationType: MultiRegistrationBehavior.Replace,
            serviceType: typeof(IDatabaseService),
            matchPredicate: d => d.ServiceType == typeof(IDatabaseService),
            newDescriptors: new[]
            {
                ServiceDescriptor.Singleton<IDatabaseService, CosmosDbService>()
            },
            expectedMatchCount: 1
        );

        var planBuilder = new RegistrationPlanBuilder()
            .WithValidationMode(ValidationMode.Strict)
            .AddOperation(operation);

        var plan = planBuilder.Build(snapshot);

        Console.WriteLine($"Plan: {plan.PlanId}");
        Console.WriteLine($"Valid: {plan.IsValid}");

        // Execute in dry run mode first
        var dryRunResult = RegistrationPlanExecutor.Execute(plan, services, dryRun: true);
        Console.WriteLine($"\nDry run: {dryRunResult.Success}");
        Console.WriteLine($"Service count after dry run: {services.Count} (unchanged)");

        // Execute for real
        var result = RegistrationPlanExecutor.Execute(plan, services, dryRun: false);
        Console.WriteLine($"\nExecution: {result.Success}");
        Console.WriteLine($"Service count after execution: {services.Count}");

        var dbDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IDatabaseService));
        Console.WriteLine($"Database service now: {dbDescriptor?.ImplementationType?.Name}");
    }

    /// <summary>
    /// Demonstrates report generation.
    /// </summary>
    public static void ReportGenerationExample()
    {
        Console.WriteLine("\n=== Report Generation Example ===\n");

        var services = new ServiceCollection();
        services.AddSingleton<IDatabaseService, SqlDatabaseService>();
        services.AddScoped<ICacheService, RedisCacheService>();

        var snapshot = ServiceGraphSnapshot.Capture(services);

        var operation = new ServiceGraphPatchOperation(
            operationId: "test-op",
            operationType: MultiRegistrationBehavior.Replace,
            serviceType: typeof(IDatabaseService),
            matchPredicate: d => d.ServiceType == typeof(IDatabaseService),
            newDescriptors: new[]
            {
                ServiceDescriptor.Singleton<IDatabaseService, CosmosDbService>()
            }
        );

        var planBuilder = new RegistrationPlanBuilder()
            .WithValidationMode(ValidationMode.Strict)
            .AddOperation(operation);

        var plan = planBuilder.Build(snapshot);

        // Generate summary
        var summary = RegistrationPlanReport.GenerateSummary(plan);
        Console.WriteLine("Summary:");
        Console.WriteLine(summary);
        Console.WriteLine();

        // Generate text report
        Console.WriteLine("Text Report:");
        var textReport = RegistrationPlanReport.GenerateTextReport(plan);
        Console.WriteLine(textReport);

        // Generate JSON report
        Console.WriteLine("\nJSON Report:");
        var jsonReport = RegistrationPlanReport.GenerateJsonReport(plan);
        Console.WriteLine(jsonReport);
    }

    /// <summary>
    /// Demonstrates all features in a complete workflow.
    /// </summary>
    public static void CompleteWorkflowExample()
    {
        Console.WriteLine("\n\n=== Complete Workflow Example ===\n");

        // Step 1: Setup services
        var services = new ServiceCollection();
        services.AddSingleton<IDatabaseService, SqlDatabaseService>();
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddTransient<IEmailService, SmtpEmailService>();
        services.AddSingleton<INotificationService, EmailNotificationService>();
        services.AddSingleton<INotificationService, SmsNotificationService>();

        Console.WriteLine("Step 1: Initial service collection created");
        Console.WriteLine($"  Total services: {services.Count}");

        // Step 2: Capture snapshot
        var snapshot = ServiceGraphSnapshot.Capture(services);
        Console.WriteLine($"\nStep 2: Snapshot captured (ID: {snapshot.SnapshotId})");

        // Step 3: Build plan
        var planBuilder = new RegistrationPlanBuilder()
            .WithValidationMode(ValidationMode.Strict)
            .WithDefaultBehavior(MultiRegistrationBehavior.Replace)
            .AddValidator(new AssignabilityValidator())
            .AddValidator(new LifetimeSafetyValidator())
            .AddValidator(new IdempotencyValidator());

        // Add operations
        planBuilder.AddOperation(new ServiceGraphPatchOperation(
            operationId: "db-experiment",
            operationType: MultiRegistrationBehavior.Replace,
            serviceType: typeof(IDatabaseService),
            matchPredicate: d => d.ServiceType == typeof(IDatabaseService),
            newDescriptors: new[]
            {
                ServiceDescriptor.Singleton<IDatabaseService, CosmosDbService>()
            },
            expectedMatchCount: 1
        ));

        var plan = planBuilder.Build(snapshot);
        Console.WriteLine($"\nStep 3: Plan built (ID: {plan.PlanId})");
        Console.WriteLine($"  Valid: {plan.IsValid}");
        Console.WriteLine($"  Operations: {plan.Operations.Count}");
        Console.WriteLine($"  Findings: {plan.ErrorCount} errors, {plan.WarningCount} warnings");

        // Step 4: Generate report
        var report = RegistrationPlanReport.GenerateSummary(plan);
        Console.WriteLine($"\nStep 4: {report}");

        // Step 5: Dry run
        var dryRunResult = RegistrationPlanExecutor.Execute(plan, services, dryRun: true);
        Console.WriteLine($"\nStep 5: Dry run completed - Success: {dryRunResult.Success}");

        // Step 6: Execute
        if (plan.IsValid)
        {
            var result = RegistrationPlanExecutor.Execute(plan, services);
            Console.WriteLine($"\nStep 6: Plan executed - Success: {result.Success}");
            
            if (result.Success)
            {
                Console.WriteLine($"  Applied {result.OperationResults.Count} operations");
                Console.WriteLine($"  Final service count: {services.Count}");
            }
            else
            {
                Console.WriteLine($"  Error: {result.ErrorMessage}");
                Console.WriteLine("  Automatic rollback performed");
            }
        }
        else
        {
            Console.WriteLine("\nStep 6: Skipped (plan invalid)");
        }
    }
}

// Sample service interfaces and implementations
public interface IDatabaseService { }
public class SqlDatabaseService : IDatabaseService { }
public class CosmosDbService : IDatabaseService { }

public interface ICacheService { }
public class RedisCacheService : ICacheService { }
public class MemoryCacheService : ICacheService { }

public interface IEmailService { }
public class SmtpEmailService : IEmailService { }

public interface INotificationService { }
public class EmailNotificationService : INotificationService { }
public class SmsNotificationService : INotificationService { }
public class PushNotificationService : INotificationService { }
public class ExperimentNotificationProxy : INotificationService { }

// Sample program to run all examples
public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            ServiceRegistrationSafetySample.BasicSnapshotAndPlanExample();
            ServiceRegistrationSafetySample.ValidationModesExample();
            ServiceRegistrationSafetySample.MultiRegistrationBehaviorsExample();
            ServiceRegistrationSafetySample.ValidatorsExample();
            ServiceRegistrationSafetySample.PlanExecutionExample();
            ServiceRegistrationSafetySample.ReportGenerationExample();
            ServiceRegistrationSafetySample.CompleteWorkflowExample();

            Console.WriteLine("\n\n✓ All examples completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
