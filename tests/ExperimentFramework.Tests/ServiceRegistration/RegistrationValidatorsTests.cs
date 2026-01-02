using ExperimentFramework.ServiceRegistration;
using ExperimentFramework.ServiceRegistration.Validators;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.ServiceRegistration;

[Feature("Validators ensure safe service registration mutations")]
public class RegistrationValidatorsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Assignability validator detects incompatible types")]
    [Fact]
    public Task Assignability_validator_detects_incompatible_types()
        => Given("a service collection with ITestService registered", () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<ITestService, TestServiceImpl>();
                return ServiceGraphSnapshot.Capture(services);
            })
            .And("an operation replacing with an incompatible type", snapshot =>
            {
                var operation = new ServiceGraphPatchOperation(
                    operationId: "test",
                    operationType: MultiRegistrationBehavior.Replace,
                    serviceType: typeof(ITestService),
                    matchPredicate: d => d.ServiceType == typeof(ITestService),
                    newDescriptors: new[]
                    {
                        new ServiceDescriptor(typeof(ITestService), typeof(IncompatibleService), ServiceLifetime.Singleton)
                    }
                );
                return (snapshot, operation);
            })
            .When("the assignability validator runs", context =>
            {
                var validator = new AssignabilityValidator();
                return validator.Validate(context.operation, context.snapshot).ToList();
            })
            .Then("it should find an error", findings => findings.Any(f => f.Severity == ValidationSeverity.Error))
            .And("the error should mention assignability", findings => findings.Any(f => f.RuleName == "Assignability"))
            .AssertPassed();

    [Scenario("Lifetime safety validator detects dangerous lifetime changes")]
    [Fact]
    public Task Lifetime_validator_detects_singleton_to_scoped_change()
        => Given("a service collection with a singleton service", () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<ITestService, TestServiceImpl>();
                return ServiceGraphSnapshot.Capture(services);
            })
            .And("an operation changing it to transient", snapshot =>
            {
                var operation = new ServiceGraphPatchOperation(
                    operationId: "test",
                    operationType: MultiRegistrationBehavior.Replace,
                    serviceType: typeof(ITestService),
                    matchPredicate: d => d.ServiceType == typeof(ITestService),
                    newDescriptors: new[]
                    {
                        ServiceDescriptor.Transient<ITestService, TestServiceImpl>()
                    }
                );
                return (snapshot, operation);
            })
            .When("the lifetime safety validator runs", context =>
            {
                var validator = new LifetimeSafetyValidator();
                return validator.Validate(context.operation, context.snapshot).ToList();
            })
            .Then("it should find an error", findings => findings.Any(f => f.Severity == ValidationSeverity.Error))
            .And("the error should mention lifetime safety", findings => findings.Any(f => f.RuleName == "LifetimeSafety"))
            .AssertPassed();

    [Scenario("Open generic validator detects arity mismatch")]
    [Fact]
    public Task Open_generic_validator_detects_arity_mismatch()
        => Given("a service collection with an open generic service", () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton(typeof(IRepository<>), typeof(Repository<>));
                return ServiceGraphSnapshot.Capture(services);
            })
            .And("an operation with wrong generic arity", snapshot =>
            {
                var operation = new ServiceGraphPatchOperation(
                    operationId: "test",
                    operationType: MultiRegistrationBehavior.Replace,
                    serviceType: typeof(IRepository<>),
                    matchPredicate: d => d.ServiceType == typeof(IRepository<>),
                    newDescriptors: new[]
                    {
                        ServiceDescriptor.Singleton(typeof(IRepository<>), typeof(WrongArityRepository<,>))
                    }
                );
                return (snapshot, operation);
            })
            .When("the open generic validator runs", context =>
            {
                var validator = new OpenGenericValidator();
                return validator.Validate(context.operation, context.snapshot).ToList();
            })
            .Then("it should find an error", findings => findings.Any(f => f.Severity == ValidationSeverity.Error))
            .And("the error should mention generic arity", findings => 
                findings.Any(f => f.RuleName == "OpenGeneric" && f.Description.Contains("arity")))
            .AssertPassed();

    [Scenario("Idempotency validator detects double-wrapping")]
    [Fact]
    public Task Idempotency_validator_detects_double_wrapping()
        => Given("a service collection with an experiment proxy already registered", () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<ITestService, TestServiceExperimentProxy>();
                return ServiceGraphSnapshot.Capture(services);
            })
            .And("an operation trying to wrap it again", snapshot =>
            {
                var operation = new ServiceGraphPatchOperation(
                    operationId: "test",
                    operationType: MultiRegistrationBehavior.Replace,
                    serviceType: typeof(ITestService),
                    matchPredicate: d => d.ServiceType == typeof(ITestService),
                    newDescriptors: new[]
                    {
                        ServiceDescriptor.Singleton<ITestService, TestServiceExperimentProxy>()
                    }
                );
                return (snapshot, operation);
            })
            .When("the idempotency validator runs", context =>
            {
                var validator = new IdempotencyValidator();
                return validator.Validate(context.operation, context.snapshot).ToList();
            })
            .Then("it should find a warning", findings => findings.Any(f => f.Severity == ValidationSeverity.Warning))
            .And("the warning should mention idempotency", findings => findings.Any(f => f.RuleName == "Idempotency"))
            .AssertPassed();

    [Scenario("Multi-registration validator warns about IEnumerable scenarios")]
    [Fact]
    public Task Multi_registration_validator_warns_about_enumerable()
        => Given("a service collection with multiple registrations", () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<ITestService, TestServiceImpl>();
                services.AddSingleton<ITestService, AnotherTestServiceImpl>();
                services.AddSingleton<ITestService, ThirdTestServiceImpl>();
                return ServiceGraphSnapshot.Capture(services);
            })
            .And("a replace operation targeting multiple registrations", snapshot =>
            {
                var operation = new ServiceGraphPatchOperation(
                    operationId: "test",
                    operationType: MultiRegistrationBehavior.Replace,
                    serviceType: typeof(ITestService),
                    matchPredicate: d => d.ServiceType == typeof(ITestService),
                    newDescriptors: new[]
                    {
                        ServiceDescriptor.Singleton<ITestService, TestServiceImpl>()
                    }
                );
                return (snapshot, operation);
            })
            .When("the multi-registration validator runs", context =>
            {
                var validator = new MultiRegistrationValidator();
                return validator.Validate(context.operation, context.snapshot).ToList();
            })
            .Then("it should find a warning", findings => findings.Any(f => f.Severity == ValidationSeverity.Warning))
            .And("the warning should mention multi-registration", findings => 
                findings.Any(f => f.RuleName == "MultiRegistration"))
            .AssertPassed();

    // Test interfaces and implementations
    private interface ITestService { }
    private class TestServiceImpl : ITestService { }
    private class AnotherTestServiceImpl : ITestService { }
    private class ThirdTestServiceImpl : ITestService { }
    private class TestServiceExperimentProxy : ITestService { }
    private class IncompatibleService { } // Does not implement ITestService

    private interface IRepository<T> { }
    private class Repository<T> : IRepository<T> { }
    private class WrongArityRepository<T1, T2> { } // Wrong arity
}
