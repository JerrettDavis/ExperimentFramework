using ExperimentFramework.ServiceRegistration;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.ServiceRegistration;

[Feature("Registration plan encapsulates plan state and findings")]
public class RegistrationPlanTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Plan tracks validity based on validation mode")]
    [Fact]
    public Task Plan_validity_reflects_findings()
        => Given("plans with different validation modes and findings", () =>
            {
                var services = new ServiceCollection();
                var snapshot = ServiceGraphSnapshot.Capture(services);
                
                var errorFinding = ValidationFinding.Error("Test", typeof(ITestService), "Error", null);
                var warningFinding = ValidationFinding.Warning("Test", typeof(ITestService), "Warning", null);
                
                // Strict mode with error - invalid
                var strictWithError = new RegistrationPlan(
                    "strict-error",
                    snapshot,
                    Array.Empty<ServiceGraphPatchOperation>(),
                    new[] { errorFinding },
                    isValid: false,
                    ValidationMode.Strict
                );
                
                // Warn mode with error - valid
                var warnWithError = new RegistrationPlan(
                    "warn-error",
                    snapshot,
                    Array.Empty<ServiceGraphPatchOperation>(),
                    new[] { errorFinding },
                    isValid: true,
                    ValidationMode.Warn
                );
                
                // No errors - valid
                var noErrors = new RegistrationPlan(
                    "no-errors",
                    snapshot,
                    Array.Empty<ServiceGraphPatchOperation>(),
                    new[] { warningFinding },
                    isValid: true,
                    ValidationMode.Strict
                );
                
                return (strictWithError, warnWithError, noErrors);
            })
            .Then("strict plan with errors should be invalid", context => !context.strictWithError.IsValid)
            .And("warn plan with errors can be valid", context => context.warnWithError.IsValid)
            .And("plan with only warnings should be valid", context => context.noErrors.IsValid)
            .AssertPassed();

    [Scenario("Plan exposes error and warning counts")]
    [Fact]
    public Task Plan_exposes_finding_counts()
        => Given("a plan with mixed findings", () =>
            {
                var services = new ServiceCollection();
                var snapshot = ServiceGraphSnapshot.Capture(services);
                
                var findings = new[]
                {
                    ValidationFinding.Error("Rule1", typeof(ITestService), "Error 1", null),
                    ValidationFinding.Error("Rule2", typeof(ITestService), "Error 2", null),
                    ValidationFinding.Warning("Rule3", typeof(ITestService), "Warning 1", null),
                    ValidationFinding.Info("Rule4", typeof(ITestService), "Info 1", null)
                };
                
                var plan = new RegistrationPlan(
                    "mixed-plan",
                    snapshot,
                    Array.Empty<ServiceGraphPatchOperation>(),
                    findings,
                    isValid: false,
                    ValidationMode.Strict
                );
                
                return plan;
            })
            .Then("error count should be 2", plan => plan.ErrorCount == 2)
            .And("warning count should be 1", plan => plan.WarningCount == 1)
            .And("has errors should be true", plan => plan.HasErrors)
            .And("has warnings should be true", plan => plan.HasWarnings)
            .AssertPassed();

    [Scenario("Plan with no findings has zero counts")]
    [Fact]
    public Task Plan_with_no_findings_has_zero_counts()
        => Given("a plan with no findings", () =>
            {
                var services = new ServiceCollection();
                var snapshot = ServiceGraphSnapshot.Capture(services);
                
                var plan = new RegistrationPlan(
                    "clean-plan",
                    snapshot,
                    Array.Empty<ServiceGraphPatchOperation>(),
                    Array.Empty<ValidationFinding>(),
                    isValid: true,
                    ValidationMode.Strict
                );
                
                return plan;
            })
            .Then("error count should be 0", plan => plan.ErrorCount == 0)
            .And("warning count should be 0", plan => plan.WarningCount == 0)
            .And("has errors should be false", plan => !plan.HasErrors)
            .And("has warnings should be false", plan => !plan.HasWarnings)
            .AssertPassed();

    [Scenario("Plan captures creation timestamp")]
    [Fact]
    public Task Plan_has_creation_timestamp()
        => Given("a newly created plan", () =>
            {
                var services = new ServiceCollection();
                var snapshot = ServiceGraphSnapshot.Capture(services);
                var beforeCreation = DateTimeOffset.UtcNow;
                
                var plan = new RegistrationPlan(
                    "timestamped-plan",
                    snapshot,
                    Array.Empty<ServiceGraphPatchOperation>(),
                    Array.Empty<ValidationFinding>(),
                    isValid: true,
                    ValidationMode.Strict
                );
                
                var afterCreation = DateTimeOffset.UtcNow;
                
                return (plan, beforeCreation, afterCreation);
            })
            .Then("creation time should be between before and after", context =>
                context.plan.CreatedAt >= context.beforeCreation &&
                context.plan.CreatedAt <= context.afterCreation)
            .AssertPassed();

    [Scenario("Plan exposes all operations in order")]
    [Fact]
    public Task Plan_exposes_operations_in_order()
        => Given("a plan with multiple operations", () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<ITestService, TestServiceImpl>();
                var snapshot = ServiceGraphSnapshot.Capture(services);
                
                var op1 = new ServiceGraphPatchOperation(
                    "op-1",
                    MultiRegistrationBehavior.Replace,
                    typeof(ITestService),
                    d => d.ServiceType == typeof(ITestService),
                    new[] { ServiceDescriptor.Singleton<ITestService, TestServiceImpl>() }.ToList()
                );
                
                var op2 = new ServiceGraphPatchOperation(
                    "op-2",
                    MultiRegistrationBehavior.Append,
                    typeof(ITestService),
                    d => d.ServiceType == typeof(ITestService),
                    new[] { ServiceDescriptor.Singleton<ITestService, TestServiceImpl>() }.ToList()
                );
                
                var plan = new RegistrationPlan(
                    "multi-op-plan",
                    snapshot,
                    new[] { op1, op2 },
                    Array.Empty<ValidationFinding>(),
                    isValid: true,
                    ValidationMode.Strict
                );
                
                return plan;
            })
            .Then("should have 2 operations", plan => plan.Operations.Count == 2)
            .And("first operation should be op-1", plan => plan.Operations[0].OperationId == "op-1")
            .And("second operation should be op-2", plan => plan.Operations[1].OperationId == "op-2")
            .AssertPassed();

    // Test interface
    private interface ITestService { }
    private class TestServiceImpl : ITestService { }
}
