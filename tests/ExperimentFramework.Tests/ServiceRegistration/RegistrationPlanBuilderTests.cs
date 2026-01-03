using ExperimentFramework.ServiceRegistration;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.ServiceRegistration;

[Feature("Registration plan builder creates validated plans")]
public class RegistrationPlanBuilderTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Plan builder creates plan with strict validation")]
    [Fact]
    public Task Plan_builder_creates_strict_validation_plan()
        => Given("a snapshot of a service collection", () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<ITestService, TestServiceImpl>();
                return ServiceGraphSnapshot.Capture(services);
            })
            .When("building a plan with strict validation", snapshot =>
            {
                var builder = new RegistrationPlanBuilder()
                    .WithValidationMode(ValidationMode.Strict);
                return builder.Build(snapshot);
            })
            .Then("plan should have strict validation mode", plan => plan.ValidationMode == ValidationMode.Strict)
            .And("plan should be valid", plan => plan.IsValid)
            .And("plan should have a unique ID", plan => !string.IsNullOrEmpty(plan.PlanId))
            .AssertPassed();

    [Scenario("Plan builder sets default multi-registration behavior")]
    [Fact]
    public Task Plan_builder_sets_default_behavior()
        => Given("a snapshot", () =>
            {
                var services = new ServiceCollection();
                return ServiceGraphSnapshot.Capture(services);
            })
            .When("building a plan with merge behavior", snapshot =>
            {
                var builder = new RegistrationPlanBuilder()
                    .WithDefaultBehavior(MultiRegistrationBehavior.Merge);
                return builder.Build(snapshot);
            })
            .Then("plan should be created", plan => plan != null)
            .And("plan should be valid", plan => plan.IsValid)
            .AssertPassed();

    [Scenario("Plan builder adds custom validators")]
    [Fact]
    public Task Plan_builder_adds_custom_validators()
        => Given("a snapshot", () =>
            {
                var services = new ServiceCollection();
                return ServiceGraphSnapshot.Capture(services);
            })
            .When("building a plan with custom validators", snapshot =>
            {
                var builder = new RegistrationPlanBuilder()
                    .AddValidator(new TestCustomValidator());
                return builder.Build(snapshot);
            })
            .Then("plan should be created", plan => plan != null)
            .AssertPassed();

    [Scenario("Plan with errors is marked invalid in strict mode")]
    [Fact]
    public Task Plan_with_errors_is_invalid_in_strict_mode()
        => Given("a snapshot with a service", () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<ITestService, TestServiceImpl>();
                return ServiceGraphSnapshot.Capture(services);
            })
            .And("an operation that will fail validation", snapshot =>
            {
                var operation = new ServiceGraphPatchOperation(
                    operationId: "test",
                    operationType: MultiRegistrationBehavior.Replace,
                    serviceType: typeof(ITestService),
                    matchPredicate: d => d.ServiceType == typeof(ITestService),
                    newDescriptors: new[]
                    {
                        // This will fail assignability check
                        new ServiceDescriptor(typeof(ITestService), typeof(IncompatibleService), ServiceLifetime.Singleton)
                    }
                );
                return (snapshot, operation);
            })
            .When("building a strict plan with that operation", context =>
            {
                var builder = new RegistrationPlanBuilder()
                    .WithValidationMode(ValidationMode.Strict)
                    .AddOperation(context.operation);
                return builder.Build(context.snapshot);
            })
            .Then("plan should be marked invalid", plan => !plan.IsValid)
            .And("plan should have error findings", plan => plan.HasErrors)
            .And("error count should be greater than zero", plan => plan.ErrorCount > 0)
            .AssertPassed();

    [Scenario("Plan with warnings is valid in warn mode")]
    [Fact]
    public Task Plan_with_warnings_is_valid_in_warn_mode()
        => Given("a snapshot", () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<ITestService, TestServiceImpl>();
                return ServiceGraphSnapshot.Capture(services);
            })
            .When("building a warn-mode plan", snapshot =>
            {
                var builder = new RegistrationPlanBuilder()
                    .WithValidationMode(ValidationMode.Warn);
                return builder.Build(snapshot);
            })
            .Then("plan should be valid even with warnings", plan => plan.IsValid)
            .AssertPassed();

    // Test classes
    private interface ITestService { }
    private class TestServiceImpl : ITestService { }
    private class IncompatibleService { }

    private class TestCustomValidator : ExperimentFramework.ServiceRegistration.Validators.IRegistrationValidator
    {
        public IEnumerable<ValidationFinding> Validate(ServiceGraphPatchOperation operation, ServiceGraphSnapshot snapshot)
        {
            yield break; // No findings
        }
    }
}
