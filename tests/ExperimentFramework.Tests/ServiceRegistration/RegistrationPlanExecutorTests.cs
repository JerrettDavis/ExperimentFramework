using ExperimentFramework.ServiceRegistration;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.ServiceRegistration;

[Feature("Registration plan executor applies plans safely with rollback")]
public class RegistrationPlanExecutorTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Executor applies valid plan successfully")]
    [Fact]
    public Task Executor_applies_valid_plan()
        => Given("a valid registration plan", () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<ITestService, TestServiceImpl>();
                
                var snapshot = ServiceGraphSnapshot.Capture(services);
                
                var operation = new ServiceGraphPatchOperation(
                    operationId: "test",
                    operationType: MultiRegistrationBehavior.Replace,
                    serviceType: typeof(ITestService),
                    matchPredicate: d => d.ServiceType == typeof(ITestService),
                    newDescriptors: new[] { ServiceDescriptor.Singleton<ITestService, NewTestServiceImpl>() }.ToList(),
                    expectedMatchCount: 1
                );
                
                var plan = new RegistrationPlan(
                    planId: "plan-1",
                    snapshot: snapshot,
                    operations: new[] { operation },
                    findings: Array.Empty<ValidationFinding>(),
                    isValid: true,
                    validationMode: ValidationMode.Strict
                );
                
                return (services, plan);
            })
            .When("executing the plan", context => RegistrationPlanExecutor.Execute(context.plan, context.services))
            .Then("execution should succeed", result => result.Success)
            .And("should have operation results", result => result.OperationResults.Count == 1)
            .And("service should be replaced", result =>
            {
                var descriptor = result.OperationResults[0];
                return descriptor.Success;
            })
            .AssertPassed();

    [Scenario("Executor rejects invalid plan")]
    [Fact]
    public Task Executor_rejects_invalid_plan()
        => Given("an invalid registration plan", () =>
            {
                var services = new ServiceCollection();
                var snapshot = ServiceGraphSnapshot.Capture(services);
                
                var findings = new[]
                {
                    ValidationFinding.Error("TestRule", typeof(ITestService), "Test error", "Fix it")
                };
                
                var plan = new RegistrationPlan(
                    planId: "plan-1",
                    snapshot: snapshot,
                    operations: Array.Empty<ServiceGraphPatchOperation>(),
                    findings: findings,
                    isValid: false,
                    validationMode: ValidationMode.Strict
                );
                
                return (services, plan);
            })
            .When("executing the plan", context => RegistrationPlanExecutor.Execute(context.plan, context.services))
            .Then("execution should fail", result => !result.Success)
            .And("should have validation findings", result => result.ValidationFindings != null)
            .And("error message should mention validation", result => result.ErrorMessage?.Contains("validation") == true)
            .AssertPassed();

    [Scenario("Executor performs dry run without mutations")]
    [Fact]
    public Task Executor_performs_dry_run()
        => Given("a service collection and plan", () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<ITestService, TestServiceImpl>();
                
                var snapshot = ServiceGraphSnapshot.Capture(services);
                
                var operation = new ServiceGraphPatchOperation(
                    operationId: "test",
                    operationType: MultiRegistrationBehavior.Replace,
                    serviceType: typeof(ITestService),
                    matchPredicate: d => d.ServiceType == typeof(ITestService),
                    newDescriptors: new[] { ServiceDescriptor.Singleton<ITestService, NewTestServiceImpl>() }.ToList(),
                    expectedMatchCount: 1
                );
                
                var plan = new RegistrationPlan(
                    planId: "plan-1",
                    snapshot: snapshot,
                    operations: new[] { operation },
                    findings: Array.Empty<ValidationFinding>(),
                    isValid: true,
                    validationMode: ValidationMode.Strict
                );
                
                var originalCount = services.Count;
                return (services, plan, originalCount);
            })
            .When("executing with dry run", context => 
            {
                var result = RegistrationPlanExecutor.Execute(context.plan, context.services, dryRun: true);
                return (result, context.services, context.originalCount);
            })
            .Then("execution should succeed", context => context.result.Success)
            .And("should be marked as dry run", context => context.result.IsDryRun)
            .And("service collection should be unchanged", context => 
                context.services.Count == context.originalCount)
            .AssertPassed();

    [Scenario("Executor rolls back on operation failure")]
    [Fact]
    public Task Executor_rolls_back_on_failure()
        => Given("a plan with a failing operation", () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<ITestService, TestServiceImpl>();
                
                var snapshot = ServiceGraphSnapshot.Capture(services);
                
                // This operation will fail because it expects 2 matches but there's only 1
                var operation = new ServiceGraphPatchOperation(
                    operationId: "test",
                    operationType: MultiRegistrationBehavior.Replace,
                    serviceType: typeof(ITestService),
                    matchPredicate: d => d.ServiceType == typeof(ITestService),
                    newDescriptors: new[] { ServiceDescriptor.Singleton<ITestService, NewTestServiceImpl>() }.ToList(),
                    expectedMatchCount: 2  // Will fail!
                );
                
                var plan = new RegistrationPlan(
                    planId: "plan-1",
                    snapshot: snapshot,
                    operations: new[] { operation },
                    findings: Array.Empty<ValidationFinding>(),
                    isValid: true,
                    validationMode: ValidationMode.Strict
                );
                
                var originalDescriptor = services.First();
                return (services, plan, originalDescriptor);
            })
            .When("executing the plan", context => 
            {
                var result = RegistrationPlanExecutor.Execute(context.plan, context.services);
                return (result, context.services, context.originalDescriptor);
            })
            .Then("execution should fail", context => !context.result.Success)
            .And("service collection should be rolled back", context =>
            {
                // Original service should still be there
                var descriptor = context.services.FirstOrDefault(d => d.ServiceType == typeof(ITestService));
                return descriptor?.ImplementationType == context.originalDescriptor.ImplementationType;
            })
            .AssertPassed();

    [Scenario("Executor handles empty plan")]
    [Fact]
    public Task Executor_handles_empty_plan()
        => Given("a plan with no operations", () =>
            {
                var services = new ServiceCollection();
                var snapshot = ServiceGraphSnapshot.Capture(services);
                
                var plan = new RegistrationPlan(
                    planId: "plan-1",
                    snapshot: snapshot,
                    operations: Array.Empty<ServiceGraphPatchOperation>(),
                    findings: Array.Empty<ValidationFinding>(),
                    isValid: true,
                    validationMode: ValidationMode.Strict
                );
                
                return (services, plan);
            })
            .When("executing the plan", context => RegistrationPlanExecutor.Execute(context.plan, context.services))
            .Then("execution should succeed", result => result.Success)
            .And("should have no operation results", result => result.OperationResults.Count == 0)
            .AssertPassed();

    // Test interfaces and implementations
    private interface ITestService { }
    private class TestServiceImpl : ITestService { }
    private class NewTestServiceImpl : ITestService { }
}
