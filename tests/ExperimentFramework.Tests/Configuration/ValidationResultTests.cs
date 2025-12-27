using ExperimentFramework.Configuration.Validation;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Configuration;

[Feature("Validation results track configuration errors and warnings")]
public class ValidationResultTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    #region ConfigurationValidationResult Tests

    [Scenario("Result with no errors is valid")]
    [Fact]
    public Task Result_with_no_errors_is_valid()
        => Given("an empty errors list", () => new ConfigurationValidationResult([]))
            .Then("is valid", result => result.IsValid)
            .And("errors is empty", result => result.Errors.Count == 0)
            .And("warnings is empty", result => !result.Warnings.Any())
            .And("fatal errors is empty", result => !result.FatalErrors.Any())
            .AssertPassed();

    [Scenario("Result with only warnings is valid")]
    [Fact]
    public Task Result_with_only_warnings_is_valid()
        => Given("warnings only", () => new[]
            {
                ConfigurationValidationError.Warning("path1", "Warning 1"),
                ConfigurationValidationError.Warning("path2", "Warning 2")
            })
            .When("creating result", errors => new ConfigurationValidationResult(errors))
            .Then("is valid", result => result.IsValid)
            .And("errors count is 2", result => result.Errors.Count == 2)
            .And("warnings count is 2", result => result.Warnings.Count() == 2)
            .And("fatal errors is empty", result => !result.FatalErrors.Any())
            .AssertPassed();

    [Scenario("Result with errors is not valid")]
    [Fact]
    public Task Result_with_errors_is_not_valid()
        => Given("errors", () => new[]
            {
                ConfigurationValidationError.Error("path1", "Error 1"),
                ConfigurationValidationError.Error("path2", "Error 2")
            })
            .When("creating result", errors => new ConfigurationValidationResult(errors))
            .Then("is not valid", result => !result.IsValid)
            .And("errors count is 2", result => result.Errors.Count == 2)
            .And("warnings is empty", result => !result.Warnings.Any())
            .And("fatal errors count is 2", result => result.FatalErrors.Count() == 2)
            .AssertPassed();

    [Scenario("Result with mixed errors and warnings")]
    [Fact]
    public Task Result_with_mixed_errors_and_warnings()
        => Given("mixed errors and warnings", () => new[]
            {
                ConfigurationValidationError.Error("path1", "Error 1"),
                ConfigurationValidationError.Warning("path2", "Warning 1"),
                ConfigurationValidationError.Error("path3", "Error 2"),
                ConfigurationValidationError.Warning("path4", "Warning 2")
            })
            .When("creating result", errors => new ConfigurationValidationResult(errors))
            .Then("is not valid", result => !result.IsValid)
            .And("total errors count is 4", result => result.Errors.Count == 4)
            .And("warnings count is 2", result => result.Warnings.Count() == 2)
            .And("fatal errors count is 2", result => result.FatalErrors.Count() == 2)
            .AssertPassed();

    [Scenario("Success returns valid result")]
    [Fact]
    public Task Success_returns_valid_result()
        => Given("the success result", () => ConfigurationValidationResult.Success)
            .Then("is valid", result => result.IsValid)
            .And("errors is empty", result => result.Errors.Count == 0)
            .AssertPassed();

    [Scenario("Success is singleton")]
    [Fact]
    public Task Success_is_singleton()
        => Given("two success results", () => (ConfigurationValidationResult.Success, ConfigurationValidationResult.Success))
            .Then("are the same instance", t => ReferenceEquals(t.Item1, t.Item2))
            .AssertPassed();

    [Scenario("Errors collection is read-only")]
    [Fact]
    public Task Errors_collection_is_readonly()
        => Given("a result with errors", () => new ConfigurationValidationResult(
                [ConfigurationValidationError.Error("path", "Error")]))
            .Then("errors is read-only", result => result.Errors is IReadOnlyList<ConfigurationValidationError>)
            .AssertPassed();

    #endregion

    #region ConfigurationValidationError Tests

    [Scenario("Error constructor sets properties")]
    [Fact]
    public Task Error_constructor_sets_properties()
        => Given("error parameters", () => new ConfigurationValidationError("test.path", "Test message", ValidationSeverity.Error))
            .Then("path is set", error => error.Path == "test.path")
            .And("message is set", error => error.Message == "Test message")
            .And("severity is error", error => error.Severity == ValidationSeverity.Error)
            .AssertPassed();

    [Scenario("Error factory creates error severity")]
    [Fact]
    public Task Error_factory_creates_error_severity()
        => Given("error from factory", () => ConfigurationValidationError.Error("path", "Error message"))
            .Then("path is set", error => error.Path == "path")
            .And("message is set", error => error.Message == "Error message")
            .And("severity is error", error => error.Severity == ValidationSeverity.Error)
            .AssertPassed();

    [Scenario("Warning factory creates warning severity")]
    [Fact]
    public Task Warning_factory_creates_warning_severity()
        => Given("warning from factory", () => ConfigurationValidationError.Warning("path", "Warning message"))
            .Then("path is set", error => error.Path == "path")
            .And("message is set", error => error.Message == "Warning message")
            .And("severity is warning", error => error.Severity == ValidationSeverity.Warning)
            .AssertPassed();

    [Scenario("Error ToString formats correctly")]
    [Fact]
    public Task Error_ToString_formats_correctly()
        => Given("an error", () => ConfigurationValidationError.Error("trials[0].serviceType", "Service type is required"))
            .When("converting to string", error => error.ToString())
            .Then("formatted correctly", str => str == "[Error] trials[0].serviceType: Service type is required")
            .AssertPassed();

    [Scenario("Warning ToString formats correctly")]
    [Fact]
    public Task Warning_ToString_formats_correctly()
        => Given("a warning", () => ConfigurationValidationError.Warning("activation.from", "Date is in the past"))
            .When("converting to string", error => error.ToString())
            .Then("formatted correctly", str => str == "[Warning] activation.from: Date is in the past")
            .AssertPassed();

    #endregion

    #region ValidationSeverity Tests

    [Scenario("ValidationSeverity has expected values")]
    [Fact]
    public Task ValidationSeverity_has_expected_values()
        => Given("validation severity enum", () => (Warning: ValidationSeverity.Warning, Error: ValidationSeverity.Error))
            .Then("warning is 0", t => (int)t.Warning == 0)
            .And("error is 1", t => (int)t.Error == 1)
            .AssertPassed();

    #endregion
}
