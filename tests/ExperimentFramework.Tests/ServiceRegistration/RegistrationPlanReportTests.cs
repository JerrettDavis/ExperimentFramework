using ExperimentFramework.ServiceRegistration;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.ServiceRegistration;

[Feature("Registration plan report generates human-readable and JSON output")]
public class RegistrationPlanReportTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Generate text report for valid plan")]
    [Fact]
    public Task Text_report_for_valid_plan()
        => Given("a valid registration plan", () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<ITestService, TestServiceImpl>();
                var snapshot = ServiceGraphSnapshot.Capture(services);
                
                var operation = new ServiceGraphPatchOperation(
                    operationId: "op-1",
                    operationType: MultiRegistrationBehavior.Replace,
                    serviceType: typeof(ITestService),
                    matchPredicate: d => d.ServiceType == typeof(ITestService),
                    newDescriptors: new[] { ServiceDescriptor.Singleton<ITestService, TestServiceImpl>() }.ToList()
                );
                
                var plan = new RegistrationPlan(
                    planId: "plan-123",
                    snapshot: snapshot,
                    operations: new[] { operation },
                    findings: Array.Empty<ValidationFinding>(),
                    isValid: true,
                    validationMode: ValidationMode.Strict
                );
                
                return plan;
            })
            .When("generating text report", plan => RegistrationPlanReport.GenerateTextReport(plan))
            .Then("report should contain plan ID", report => report.Contains("plan-123"))
            .And("report should mention validation mode", report => report.Contains("Strict"))
            .And("report should show valid status", report => report.Contains("Valid: YES"))
            .And("report should list operations", report => report.Contains("Patch Operations"))
            .AssertPassed();

    [Scenario("Generate text report for plan with errors")]
    [Fact]
    public Task Text_report_shows_errors()
        => Given("a plan with validation errors", () =>
            {
                var services = new ServiceCollection();
                var snapshot = ServiceGraphSnapshot.Capture(services);
                
                var findings = new[]
                {
                    ValidationFinding.Error("Assignability", typeof(ITestService), "Type mismatch", "Fix the type"),
                    ValidationFinding.Warning("LifetimeSafety", typeof(ITestService), "Lifetime issue", "Review lifetime")
                };
                
                var plan = new RegistrationPlan(
                    planId: "plan-456",
                    snapshot: snapshot,
                    operations: Array.Empty<ServiceGraphPatchOperation>(),
                    findings: findings,
                    isValid: false,
                    validationMode: ValidationMode.Strict
                );
                
                return plan;
            })
            .When("generating text report", plan => RegistrationPlanReport.GenerateTextReport(plan))
            .Then("report should show invalid status", report => report.Contains("Valid: NO"))
            .And("report should list errors", report => report.Contains("[ERROR]"))
            .And("report should list warnings", report => report.Contains("[WARN]"))
            .And("report should show error count", report => report.Contains("Errors: 1"))
            .And("report should show warning count", report => report.Contains("Warnings: 1"))
            .AssertPassed();

    [Scenario("Generate JSON report")]
    [Fact]
    public Task JSON_report_is_valid_json()
        => Given("a registration plan", () =>
            {
                var services = new ServiceCollection();
                var snapshot = ServiceGraphSnapshot.Capture(services);
                
                var plan = new RegistrationPlan(
                    planId: "plan-789",
                    snapshot: snapshot,
                    operations: Array.Empty<ServiceGraphPatchOperation>(),
                    findings: Array.Empty<ValidationFinding>(),
                    isValid: true,
                    validationMode: ValidationMode.Warn
                );
                
                return plan;
            })
            .When("generating JSON report", plan => RegistrationPlanReport.GenerateJsonReport(plan))
            .Then("report should be valid JSON", report =>
            {
                try
                {
                    System.Text.Json.JsonDocument.Parse(report);
                    return true;
                }
                catch
                {
                    return false;
                }
            })
            .And("report should contain plan ID", report => report.Contains("plan-789"))
            .And("report should be formatted", report => report.Contains("\n"))
            .AssertPassed();

    [Scenario("Generate summary report")]
    [Fact]
    public Task Summary_report_is_concise()
        => Given("a registration plan", () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<ITestService, TestServiceImpl>();
                var snapshot = ServiceGraphSnapshot.Capture(services);
                
                var operation = new ServiceGraphPatchOperation(
                    operationId: "op-1",
                    operationType: MultiRegistrationBehavior.Replace,
                    serviceType: typeof(ITestService),
                    matchPredicate: d => d.ServiceType == typeof(ITestService),
                    newDescriptors: new[] { ServiceDescriptor.Singleton<ITestService, TestServiceImpl>() }.ToList()
                );
                
                var findings = new[]
                {
                    ValidationFinding.Warning("Test", typeof(ITestService), "Warning", null)
                };
                
                var plan = new RegistrationPlan(
                    planId: "plan-abc",
                    snapshot: snapshot,
                    operations: new[] { operation },
                    findings: findings,
                    isValid: true,
                    validationMode: ValidationMode.Warn
                );
                
                return plan;
            })
            .When("generating summary", plan => RegistrationPlanReport.GenerateSummary(plan))
            .Then("summary should contain plan ID", summary => summary.Contains("plan-abc"))
            .And("summary should show operation count", summary => summary.Contains("1 operations"))
            .And("summary should show warning count", summary => summary.Contains("1 warnings"))
            .And("summary should be single line", summary => !summary.Contains("\n") || summary.Split('\n').Length <= 1)
            .AssertPassed();

    [Scenario("Text report shows all operation details")]
    [Fact]
    public Task Text_report_shows_operation_details()
        => Given("a plan with detailed operations", () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<ITestService, TestServiceImpl>();
                var snapshot = ServiceGraphSnapshot.Capture(services);
                
                var metadata = new OperationMetadata(
                    "Test operation description",
                    new Dictionary<string, string> { ["Key1"] = "Value1" }
                );
                
                var operation = new ServiceGraphPatchOperation(
                    operationId: "detailed-op",
                    operationType: MultiRegistrationBehavior.Insert,
                    serviceType: typeof(ITestService),
                    matchPredicate: d => d.ServiceType == typeof(ITestService),
                    newDescriptors: new[] { ServiceDescriptor.Singleton<ITestService, TestServiceImpl>() }.ToList(),
                    expectedMatchCount: 1,
                    allowNoMatches: false,
                    metadata: metadata
                );
                
                var plan = new RegistrationPlan(
                    planId: "detailed-plan",
                    snapshot: snapshot,
                    operations: new[] { operation },
                    findings: Array.Empty<ValidationFinding>(),
                    isValid: true,
                    validationMode: ValidationMode.Strict
                );
                
                return plan;
            })
            .When("generating text report", plan => RegistrationPlanReport.GenerateTextReport(plan))
            .Then("report should show operation ID", report => report.Contains("detailed-op"))
            .And("report should show operation type", report => report.Contains("Insert"))
            .And("report should show service type", report => report.Contains("ITestService"))
            .And("report should show expected matches", report => report.Contains("Expected Matches: 1"))
            .And("report should show description", report => report.Contains("Test operation description"))
            .AssertPassed();

    // Test interface
    private interface ITestService { }
    private class TestServiceImpl : ITestService { }
}
