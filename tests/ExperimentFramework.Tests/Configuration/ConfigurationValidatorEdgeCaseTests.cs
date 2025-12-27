using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Configuration;

[Feature("ConfigurationValidator validates experiment configurations for correctness")]
public class ConfigurationValidatorEdgeCaseTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    #region Helper Methods

    private static ConfigurationValidator CreateValidator() => new();

    private static ExperimentFrameworkConfigurationRoot CreateTrialConfig(
        string? serviceType = "IService",
        SelectionModeConfig? selectionMode = null,
        ConditionConfig? control = null,
        List<ConditionConfig>? conditions = null,
        ErrorPolicyConfig? errorPolicy = null,
        ActivationConfig? activation = null)
    {
        return new ExperimentFrameworkConfigurationRoot
        {
            Trials =
            [
                new TrialConfig
                {
                    ServiceType = serviceType!,
                    SelectionMode = selectionMode ?? new SelectionModeConfig { Type = "featureFlag" },
                    Control = control ?? new ConditionConfig { Key = "control", ImplementationType = "Service" },
                    Conditions = conditions,
                    ErrorPolicy = errorPolicy,
                    Activation = activation
                }
            ]
        };
    }

    #endregion

    #region Trial Validation Edge Cases

    [Scenario("Trial with null service type returns error")]
    [Fact]
    public Task Trial_with_null_service_type_returns_error()
        => Given("a trial config with null service type", () => CreateTrialConfig(serviceType: null!))
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is not valid", result => !result.IsValid)
            .AssertPassed();

    [Scenario("Trial with whitespace service type returns error")]
    [Fact]
    public Task Trial_with_whitespace_service_type_returns_error()
        => Given("a trial config with whitespace service type", () => CreateTrialConfig(serviceType: "   "))
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is not valid", result => !result.IsValid)
            .AssertPassed();

    [Scenario("Trial with null control returns error")]
    [Fact]
    public Task Trial_with_null_control_returns_error()
        => Given("a trial config with null control", () => new ExperimentFrameworkConfigurationRoot
            {
                Trials =
                [
                    new TrialConfig
                    {
                        ServiceType = "IService",
                        SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                        Control = null!
                    }
                ]
            })
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is not valid", result => !result.IsValid)
            .AssertPassed();

    [Scenario("Trial with empty control key returns error")]
    [Fact]
    public Task Trial_with_empty_control_key_returns_error()
        => Given("a trial config with empty control key", () => CreateTrialConfig(
            control: new ConditionConfig { Key = "", ImplementationType = "Service" }))
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is not valid", result => !result.IsValid)
            .AssertPassed();

    [Scenario("Trial with empty control implementation type returns error")]
    [Fact]
    public Task Trial_with_empty_control_implementation_type_returns_error()
        => Given("a trial config with empty implementation type", () => CreateTrialConfig(
            control: new ConditionConfig { Key = "control", ImplementationType = "" }))
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is not valid", result => !result.IsValid)
            .AssertPassed();

    [Scenario("Trial with empty condition key returns error")]
    [Fact]
    public Task Trial_with_empty_condition_key_returns_error()
        => Given("a trial config with empty condition key", () => CreateTrialConfig(
            conditions: [new ConditionConfig { Key = "", ImplementationType = "OtherService" }]))
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is not valid", result => !result.IsValid)
            .AssertPassed();

    [Scenario("Trial with condition key matching control returns duplicate error")]
    [Fact]
    public Task Trial_with_condition_key_matching_control_returns_duplicate_error()
        => Given("a trial config with duplicate key", () => CreateTrialConfig(
            control: new ConditionConfig { Key = "control", ImplementationType = "Service" },
            conditions: [new ConditionConfig { Key = "control", ImplementationType = "OtherService" }]))
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is not valid", result => !result.IsValid)
            .And("contains duplicate error", result => result.Errors.Any(e => e.Message.Contains("Duplicate")))
            .AssertPassed();

    #endregion

    #region Selection Mode Validation Edge Cases

    [Scenario("Null selection mode returns error")]
    [Fact]
    public Task Null_selection_mode_returns_error()
        => Given("a trial config with null selection mode", () => new ExperimentFrameworkConfigurationRoot
            {
                Trials =
                [
                    new TrialConfig
                    {
                        ServiceType = "IService",
                        SelectionMode = null!,
                        Control = new ConditionConfig { Key = "control", ImplementationType = "Service" }
                    }
                ]
            })
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is not valid", result => !result.IsValid)
            .AssertPassed();

    [Scenario("Empty selection mode type returns error")]
    [Fact]
    public Task Empty_selection_mode_type_returns_error()
        => Given("a trial config with empty selection mode type", () => CreateTrialConfig(
            selectionMode: new SelectionModeConfig { Type = "" }))
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is not valid", result => !result.IsValid)
            .AssertPassed();

    [Scenario("Feature flag without flag name is valid")]
    [Fact]
    public Task Feature_flag_without_flag_name_is_valid()
        => Given("a trial config with featureFlag and no flagName", () => CreateTrialConfig(
            selectionMode: new SelectionModeConfig { Type = "featureFlag" }))
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is valid", result => result.IsValid)
            .AssertPassed();

    [Scenario("Configuration key without key is valid")]
    [Fact]
    public Task Configuration_key_without_key_is_valid()
        => Given("a trial config with configurationKey and no key", () => CreateTrialConfig(
            selectionMode: new SelectionModeConfig { Type = "configurationKey" }))
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is valid", result => result.IsValid)
            .AssertPassed();

    [Scenario("All valid selection modes are valid")]
    [Theory]
    [InlineData("featureFlag")]
    [InlineData("configurationKey")]
    [InlineData("variantFeatureFlag")]
    [InlineData("openFeature")]
    [InlineData("stickyRouting")]
    public Task All_valid_selection_modes_are_valid(string mode)
        => Given($"a trial config with {mode} selection mode", () => CreateTrialConfig(
            selectionMode: new SelectionModeConfig { Type = mode }))
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is valid", result => result.IsValid)
            .AssertPassed();

    [Scenario("Custom selection mode with identifier is valid")]
    [Fact]
    public Task Custom_selection_mode_with_identifier_is_valid()
        => Given("a trial config with custom selection mode", () => CreateTrialConfig(
            selectionMode: new SelectionModeConfig { Type = "custom", ModeIdentifier = "myMode" }))
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is valid", result => result.IsValid)
            .AssertPassed();

    #endregion

    #region Error Policy Validation Edge Cases

    [Scenario("Fallback to policy without key returns result")]
    [Fact]
    public Task Fallback_to_policy_without_key_returns_result()
        => Given("a trial config with fallbackTo and no key", () => CreateTrialConfig(
            errorPolicy: new ErrorPolicyConfig { Type = "fallbackTo" }))
            .When("validating", config => CreateValidator().Validate(config))
            .Then("result is not null", result => result != null)
            .AssertPassed();

    [Scenario("Try in order with empty keys returns result")]
    [Fact]
    public Task Try_in_order_with_empty_keys_returns_result()
        => Given("a trial config with tryInOrder and empty keys", () => CreateTrialConfig(
            errorPolicy: new ErrorPolicyConfig { Type = "tryInOrder", FallbackKeys = [] }))
            .When("validating", config => CreateValidator().Validate(config))
            .Then("result is not null", result => result != null)
            .AssertPassed();

    [Scenario("All valid error policies are valid")]
    [Theory]
    [InlineData("throw")]
    [InlineData("fallbackToControl")]
    [InlineData("tryAny")]
    public Task All_valid_error_policies_are_valid(string policy)
        => Given($"a trial config with {policy} error policy", () => CreateTrialConfig(
            errorPolicy: new ErrorPolicyConfig { Type = policy }))
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is valid", result => result.IsValid)
            .AssertPassed();

    #endregion

    #region Activation Validation Edge Cases

    [Scenario("Activation with from after until returns error")]
    [Fact]
    public Task Activation_with_from_after_until_returns_error()
        => Given("a trial config with invalid date range", () => CreateTrialConfig(
            activation: new ActivationConfig
            {
                From = DateTimeOffset.Now.AddDays(10),
                Until = DateTimeOffset.Now.AddDays(5)
            }))
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is not valid", result => !result.IsValid)
            .And("contains before error", result => result.Errors.Any(e => e.Message.Contains("before")))
            .AssertPassed();

    [Scenario("Activation with only from date is valid")]
    [Fact]
    public Task Activation_with_only_from_date_is_valid()
        => Given("a trial config with only from date", () => CreateTrialConfig(
            activation: new ActivationConfig { From = DateTimeOffset.Now }))
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is valid", result => result.IsValid)
            .AssertPassed();

    [Scenario("Activation with only until date is valid")]
    [Fact]
    public Task Activation_with_only_until_date_is_valid()
        => Given("a trial config with only until date", () => CreateTrialConfig(
            activation: new ActivationConfig { Until = DateTimeOffset.Now.AddDays(30) }))
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is valid", result => result.IsValid)
            .AssertPassed();

    #endregion

    #region Experiment Validation Edge Cases

    [Scenario("Experiment with empty name returns error")]
    [Fact]
    public Task Experiment_with_empty_name_returns_error()
        => Given("an experiment config with empty name", () => new ExperimentFrameworkConfigurationRoot
            {
                Experiments = [new ExperimentConfig { Name = "", Trials = [] }]
            })
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is not valid", result => !result.IsValid)
            .AssertPassed();

    [Scenario("Experiment with empty trials returns result")]
    [Fact]
    public Task Experiment_with_empty_trials_returns_result()
        => Given("an experiment config with empty trials", () => new ExperimentFrameworkConfigurationRoot
            {
                Experiments = [new ExperimentConfig { Name = "test-experiment", Trials = [] }]
            })
            .When("validating", config => CreateValidator().Validate(config))
            .Then("result is not null", result => result != null)
            .AssertPassed();

    [Scenario("Three duplicate experiment names returns multiple errors")]
    [Fact]
    public Task Three_duplicate_experiment_names_returns_multiple_errors()
        => Given("three experiments with same name", () => new ExperimentFrameworkConfigurationRoot
            {
                Experiments =
                [
                    new ExperimentConfig { Name = "same-name", Trials = [] },
                    new ExperimentConfig { Name = "same-name", Trials = [] },
                    new ExperimentConfig { Name = "same-name", Trials = [] }
                ]
            })
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is not valid", result => !result.IsValid)
            .And("contains duplicate error", result => result.Errors.Any(e => e.Message.Contains("Duplicate")))
            .AssertPassed();

    [Scenario("Experiment names are case insensitive for duplicate detection")]
    [Fact]
    public Task Experiment_names_are_case_insensitive()
        => Given("two experiments with same name different case", () => new ExperimentFrameworkConfigurationRoot
            {
                Experiments =
                [
                    new ExperimentConfig { Name = "Test-Experiment", Trials = [] },
                    new ExperimentConfig { Name = "test-experiment", Trials = [] }
                ]
            })
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is not valid", result => !result.IsValid)
            .AssertPassed();

    #endregion

    #region Decorator Validation Edge Cases

    [Scenario("Valid decorators are valid")]
    [Theory]
    [InlineData("logging")]
    [InlineData("timeout")]
    [InlineData("circuitBreaker")]
    [InlineData("outcomeCollection")]
    public Task Valid_decorators_are_valid(string decorator)
        => Given($"a config with {decorator} decorator", () => new ExperimentFrameworkConfigurationRoot
            {
                Decorators = [new DecoratorConfig { Type = decorator }]
            })
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is valid", result => result.IsValid)
            .AssertPassed();

    [Scenario("Custom decorator with type name is valid")]
    [Fact]
    public Task Custom_decorator_with_type_name_is_valid()
        => Given("a custom decorator config", () => new ExperimentFrameworkConfigurationRoot
            {
                Decorators = [new DecoratorConfig { Type = "custom", TypeName = "MyCustomDecorator, MyAssembly" }]
            })
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is valid", result => result.IsValid)
            .AssertPassed();

    [Scenario("Empty decorator type returns error")]
    [Fact]
    public Task Empty_decorator_type_returns_error()
        => Given("a config with empty decorator type", () => new ExperimentFrameworkConfigurationRoot
            {
                Decorators = [new DecoratorConfig { Type = "" }]
            })
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is not valid", result => !result.IsValid)
            .AssertPassed();

    #endregion

    #region Multiple Issues

    [Scenario("Multiple issues reports all errors")]
    [Fact]
    public Task Multiple_issues_reports_all_errors()
        => Given("a config with multiple issues", () => new ExperimentFrameworkConfigurationRoot
            {
                Trials =
                [
                    new TrialConfig
                    {
                        ServiceType = "",
                        SelectionMode = new SelectionModeConfig { Type = "invalid" },
                        Control = new ConditionConfig { Key = "", ImplementationType = "" }
                    }
                ],
                Experiments = [new ExperimentConfig { Name = "", Trials = [] }]
            })
            .When("validating", config => CreateValidator().Validate(config))
            .Then("is not valid", result => !result.IsValid)
            .And("has multiple errors", result => result.Errors.Count >= 2)
            .AssertPassed();

    #endregion
}
