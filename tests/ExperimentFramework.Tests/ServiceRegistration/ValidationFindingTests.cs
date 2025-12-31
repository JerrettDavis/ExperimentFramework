using ExperimentFramework.ServiceRegistration;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.ServiceRegistration;

[Feature("Validation finding represents validation issues with severity")]
public class ValidationFindingTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Create error finding with all properties")]
    [Fact]
    public Task Error_finding_has_correct_properties()
        => Given("error finding properties", () => (
                severity: ValidationSeverity.Error,
                ruleName: "TestRule",
                serviceType: typeof(ITestService),
                description: "Test error description",
                recommendedAction: "Fix this issue"
            ))
            .When("creating an error finding", props =>
                ValidationFinding.Error(props.ruleName, props.serviceType, props.description, props.recommendedAction))
            .Then("severity should be Error", finding => finding.Severity == ValidationSeverity.Error)
            .And("rule name should match", finding => finding.RuleName == "TestRule")
            .And("service type should match", finding => finding.ServiceType == typeof(ITestService))
            .And("description should match", finding => finding.Description == "Test error description")
            .And("recommended action should match", finding => finding.RecommendedAction == "Fix this issue")
            .AssertPassed();

    [Scenario("Create warning finding")]
    [Fact]
    public Task Warning_finding_has_warning_severity()
        => Given("warning finding parameters", () => ("WarningRule", typeof(ITestService), "Warning message"))
            .When("creating a warning finding", props =>
                ValidationFinding.Warning(props.Item1, props.Item2, props.Item3))
            .Then("severity should be Warning", finding => finding.Severity == ValidationSeverity.Warning)
            .And("rule name should match", finding => finding.RuleName == "WarningRule")
            .And("recommended action should be null", finding => finding.RecommendedAction == null)
            .AssertPassed();

    [Scenario("Create info finding")]
    [Fact]
    public Task Info_finding_has_info_severity()
        => Given("info finding parameters", () => ("InfoRule", typeof(ITestService), "Info message"))
            .When("creating an info finding", props =>
                ValidationFinding.Info(props.Item1, props.Item2, props.Item3))
            .Then("severity should be Info", finding => finding.Severity == ValidationSeverity.Info)
            .And("rule name should match", finding => finding.RuleName == "InfoRule")
            .AssertPassed();

    [Scenario("Finding with no recommended action")]
    [Fact]
    public Task Finding_without_recommended_action()
        => Given("finding without recommended action", () =>
                ValidationFinding.Error("NoActionRule", typeof(ITestService), "Error without action", null))
            .Then("recommended action should be null", finding => finding.RecommendedAction == null)
            .AssertPassed();

    [Scenario("Finding constructor validates required parameters")]
    [Fact]
    public Task Finding_validates_required_parameters()
        => Given("invalid parameters", () => (
                nullRuleName: (string?)null,
                nullServiceType: (Type?)null,
                nullDescription: (string?)null
            ))
            .Then("null rule name should throw", props =>
            {
                try
                {
                    var _ = new ValidationFinding(ValidationSeverity.Error, props.nullRuleName!, typeof(ITestService), "desc");
                    return false;
                }
                catch (ArgumentNullException)
                {
                    return true;
                }
            })
            .And("null service type should throw", props =>
            {
                try
                {
                    var _ = new ValidationFinding(ValidationSeverity.Error, "rule", props.nullServiceType!, "desc");
                    return false;
                }
                catch (ArgumentNullException)
                {
                    return true;
                }
            })
            .And("null description should throw", props =>
            {
                try
                {
                    var _ = new ValidationFinding(ValidationSeverity.Error, "rule", typeof(ITestService), props.nullDescription!);
                    return false;
                }
                catch (ArgumentNullException)
                {
                    return true;
                }
            })
            .AssertPassed();

    [Scenario("Severity enum has correct ordering")]
    [Fact]
    public Task Severity_enum_ordering()
        => Given("severity enum values", () => (
                info: ValidationSeverity.Info,
                warning: ValidationSeverity.Warning,
                error: ValidationSeverity.Error
            ))
            .Then("Info should be less than Warning", values => values.info < values.warning)
            .And("Warning should be less than Error", values => values.warning < values.error)
            .And("Info should be less than Error", values => values.info < values.error)
            .AssertPassed();

    // Test interface
    private interface ITestService { }
}
