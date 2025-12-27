using ExperimentFramework.Configuration.Exceptions;
using ExperimentFramework.Configuration.Validation;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Configuration;

[Feature("Configuration exceptions provide detailed error information")]
public class ExceptionTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    #region ExperimentConfigurationException Tests

    [Scenario("Exception with message sets message correctly")]
    [Fact]
    public Task Exception_with_message_sets_message()
        => Given("a message", () => "Test message")
            .When("creating exception", msg => new ExperimentConfigurationException(msg))
            .Then("message is set", ex => ex.Message == "Test message")
            .And("validation errors is empty", ex => ex.ValidationErrors.Count == 0)
            .AssertPassed();

    [Scenario("Exception with inner exception sets correctly")]
    [Fact]
    public Task Exception_with_inner_exception_sets_correctly()
        => Given("an inner exception", () => new InvalidOperationException("Inner"))
            .When("creating exception with inner", inner => new ExperimentConfigurationException("Outer message", inner))
            .Then("message is set", ex => ex.Message == "Outer message")
            .And("inner exception is set", ex => ex.InnerException!.Message == "Inner")
            .And("validation errors is empty", ex => ex.ValidationErrors.Count == 0)
            .AssertPassed();

    [Scenario("Exception with errors formats message correctly")]
    [Fact]
    public Task Exception_with_errors_formats_message()
        => Given("validation errors", () => new[]
            {
                ConfigurationValidationError.Error("trials[0].serviceType", "Service type is required"),
                ConfigurationValidationError.Error("trials[0].control", "Control is required")
            })
            .When("creating exception", errors => new ExperimentConfigurationException("Configuration invalid", errors))
            .Then("message contains base message", ex => ex.Message.Contains("Configuration invalid"))
            .And("message contains validation errors header", ex => ex.Message.Contains("Validation errors:"))
            .And("message contains first error path", ex => ex.Message.Contains("trials[0].serviceType"))
            .And("message contains second error path", ex => ex.Message.Contains("trials[0].control"))
            .And("validation errors count is 2", ex => ex.ValidationErrors.Count == 2)
            .AssertPassed();

    [Scenario("Exception with errors and warnings formats both")]
    [Fact]
    public Task Exception_with_errors_and_warnings_formats_both()
        => Given("mixed errors and warnings", () => new[]
            {
                ConfigurationValidationError.Error("trials[0].serviceType", "Service type is required"),
                ConfigurationValidationError.Warning("trials[0].activation", "Activation dates in the past")
            })
            .When("creating exception", errors => new ExperimentConfigurationException("Configuration invalid", errors))
            .Then("message contains errors header", ex => ex.Message.Contains("Validation errors:"))
            .And("message contains warnings header", ex => ex.Message.Contains("Warnings:"))
            .And("message contains error message", ex => ex.Message.Contains("Service type is required"))
            .And("message contains warning message", ex => ex.Message.Contains("Activation dates in the past"))
            .AssertPassed();

    [Scenario("Exception with empty errors has simple message")]
    [Fact]
    public Task Exception_with_empty_errors_has_simple_message()
        => Given("empty errors array", Array.Empty<ConfigurationValidationError>)
            .When("creating exception", errors => new ExperimentConfigurationException("Configuration invalid", errors))
            .Then("message is the base message", ex => ex.Message == "Configuration invalid")
            .AssertPassed();

    [Scenario("Exception with only warnings formats correctly")]
    [Fact]
    public Task Exception_with_only_warnings_formats_correctly()
        => Given("only warnings", () => new[]
            {
                ConfigurationValidationError.Warning("path1", "Warning 1"),
                ConfigurationValidationError.Warning("path2", "Warning 2")
            })
            .When("creating exception", errors => new ExperimentConfigurationException("Configuration issues", errors))
            .Then("message contains warnings header", ex => ex.Message.Contains("Warnings:"))
            .And("message contains warning 1", ex => ex.Message.Contains("Warning 1"))
            .And("message contains warning 2", ex => ex.Message.Contains("Warning 2"))
            .AssertPassed();

    #endregion

    #region TypeResolutionException Tests

    [Scenario("TypeResolutionException with type name sets properties")]
    [Fact]
    public Task TypeResolutionException_with_type_name()
        => Given("a type name", () => "MyType")
            .When("creating exception", typeName => new TypeResolutionException(typeName))
            .Then("type name is set", ex => ex.TypeName == "MyType")
            .And("configuration path is null", ex => ex.ConfigurationPath == null)
            .And("message contains type name", ex => ex.Message.Contains("MyType"))
            .And("message indicates resolution failure", ex => ex.Message.Contains("Could not resolve type"))
            .AssertPassed();

    [Scenario("TypeResolutionException with type name and message")]
    [Fact]
    public Task TypeResolutionException_with_type_name_and_message()
        => Given("type name and message", () => ("MyType", "Custom message"))
            .When("creating exception", t => new TypeResolutionException(t.Item1, t.Item2))
            .Then("type name is set", ex => ex.TypeName == "MyType")
            .And("message contains type name", ex => ex.Message.Contains("MyType"))
            .And("message contains custom message", ex => ex.Message.Contains("Custom message"))
            .AssertPassed();

    [Scenario("TypeResolutionException with path sets properties")]
    [Fact]
    public Task TypeResolutionException_with_path()
        => Given("type name, path, and message", () => ("MyType", "trials[0].serviceType", "Type not found"))
            .When("creating exception", t => new TypeResolutionException(t.Item1, t.Item2, t.Item3))
            .Then("type name is set", ex => ex.TypeName == "MyType")
            .And("configuration path is set", ex => ex.ConfigurationPath == "trials[0].serviceType")
            .And("message contains path", ex => ex.Message.Contains("trials[0].serviceType"))
            .AssertPassed();

    [Scenario("TypeResolutionException with inner exception")]
    [Fact]
    public Task TypeResolutionException_with_inner_exception()
        => Given("inner exception", () => new TypeLoadException("Load failed"))
            .When("creating exception", inner => new TypeResolutionException("MyType", inner))
            .Then("type name is set", ex => ex.TypeName == "MyType")
            .And("inner exception is set", ex => ex.InnerException is TypeLoadException)
            .And("message contains inner message", ex => ex.Message.Contains("Load failed"))
            .AssertPassed();

    [Scenario("TypeResolutionException inherits from ExperimentConfigurationException")]
    [Fact]
    public Task TypeResolutionException_inherits_correctly()
        => Given("a type resolution exception", () => new TypeResolutionException("MyType"))
            .Then("is ExperimentConfigurationException", ex => ex is ExperimentConfigurationException)
            .AssertPassed();

    #endregion

    #region ConfigurationLoadException Tests

    [Scenario("ConfigurationLoadException with message")]
    [Fact]
    public Task ConfigurationLoadException_with_message()
        => Given("a message", () => "Load failed")
            .When("creating exception", msg => new ConfigurationLoadException(msg))
            .Then("message is set", ex => ex.Message == "Load failed")
            .And("file path is null", ex => ex.FilePath == null)
            .AssertPassed();

    [Scenario("ConfigurationLoadException with file path")]
    [Fact]
    public Task ConfigurationLoadException_with_file_path()
        => Given("file path and message", () => ("/path/to/file.yaml", "Parse error"))
            .When("creating exception", t => new ConfigurationLoadException(t.Item1, t.Item2))
            .Then("file path is set", ex => ex.FilePath == "/path/to/file.yaml")
            .And("message contains file path", ex => ex.Message.Contains("/path/to/file.yaml"))
            .And("message contains error", ex => ex.Message.Contains("Parse error"))
            .AssertPassed();

    [Scenario("ConfigurationLoadException with inner exception")]
    [Fact]
    public Task ConfigurationLoadException_with_inner_exception()
        => Given("file path, message, and inner exception", () =>
            ("/path/to/file.yaml", "Parse error", new FormatException("Invalid format")))
            .When("creating exception", t => new ConfigurationLoadException(t.Item1, t.Item2, t.Item3))
            .Then("file path is set", ex => ex.FilePath == "/path/to/file.yaml")
            .And("inner exception is set", ex => ex.InnerException is FormatException)
            .AssertPassed();

    [Scenario("ConfigurationLoadException inherits from ExperimentConfigurationException")]
    [Fact]
    public Task ConfigurationLoadException_inherits_correctly()
        => Given("a configuration load exception", () => new ConfigurationLoadException("Load failed"))
            .Then("is ExperimentConfigurationException", ex => ex is ExperimentConfigurationException)
            .AssertPassed();

    #endregion
}
