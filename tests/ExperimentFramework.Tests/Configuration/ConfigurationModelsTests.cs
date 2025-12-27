using ExperimentFramework.Configuration.Models;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Configuration;

[Feature("Configuration models represent YAML/JSON experiment definitions")]
public class ConfigurationModelsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    #region Helper Methods

    private static TrialConfig CreateValidTrialConfig(string serviceType = "IService")
        => new()
        {
            ServiceType = serviceType,
            SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
            Control = new ConditionConfig { Key = "control", ImplementationType = "ServiceImpl" }
        };

    private static ExperimentConfig CreateValidExperimentConfig(string name = "test-experiment")
        => new()
        {
            Name = name,
            Trials = []
        };

    private static HypothesisConfig CreateValidHypothesisConfig(string name = "test-hypothesis")
        => new()
        {
            Name = name,
            Type = "superiority",
            NullHypothesis = "No difference",
            AlternativeHypothesis = "Treatment is better",
            PrimaryEndpoint = new EndpointConfig { Name = "response_time", OutcomeType = "continuous" },
            ExpectedEffectSize = 0.2,
            SuccessCriteria = new SuccessCriteriaConfig()
        };

    #endregion

    #region ExperimentFrameworkConfigurationRoot Tests

    [Scenario("Configuration root has null defaults")]
    [Fact]
    public Task ConfigurationRoot_has_null_defaults()
        => Given("a new configuration root", () => new ExperimentFrameworkConfigurationRoot())
            .Then("settings is null", root => root.Settings == null)
            .And("decorators is null", root => root.Decorators == null)
            .And("trials is null", root => root.Trials == null)
            .And("experiments is null", root => root.Experiments == null)
            .And("configuration paths is null", root => root.ConfigurationPaths == null)
            .AssertPassed();

    [Scenario("Configuration root can set all properties")]
    [Fact]
    public Task ConfigurationRoot_can_set_all_properties()
        => Given("a configuration root with all properties", () => new ExperimentFrameworkConfigurationRoot
            {
                Settings = new FrameworkSettingsConfig { ProxyStrategy = "dispatchProxy" },
                Decorators = [new DecoratorConfig { Type = "logging" }],
                Trials = [CreateValidTrialConfig()],
                Experiments = [CreateValidExperimentConfig()],
                ConfigurationPaths = ["path1.yaml"]
            })
            .Then("settings is set", root => root.Settings != null)
            .And("decorators has one item", root => root.Decorators!.Count == 1)
            .And("trials has one item", root => root.Trials!.Count == 1)
            .And("experiments has one item", root => root.Experiments!.Count == 1)
            .And("configuration paths has one item", root => root.ConfigurationPaths!.Count == 1)
            .AssertPassed();

    #endregion

    #region FrameworkSettingsConfig Tests

    [Scenario("Framework settings has correct defaults")]
    [Fact]
    public Task FrameworkSettings_has_correct_defaults()
        => Given("a new framework settings config", () => new FrameworkSettingsConfig())
            .Then("proxy strategy defaults to sourceGenerators", settings => settings.ProxyStrategy == "sourceGenerators")
            .And("naming convention defaults to default", settings => settings.NamingConvention == "default")
            .AssertPassed();

    [Scenario("Framework settings can be customized")]
    [Fact]
    public Task FrameworkSettings_can_be_customized()
        => Given("customized framework settings", () => new FrameworkSettingsConfig
            {
                ProxyStrategy = "dispatchProxy",
                NamingConvention = "camelCase"
            })
            .Then("proxy strategy is set", settings => settings.ProxyStrategy == "dispatchProxy")
            .And("naming convention is set", settings => settings.NamingConvention == "camelCase")
            .AssertPassed();

    #endregion

    #region DecoratorConfig Tests

    [Scenario("Decorator config with required type")]
    [Fact]
    public Task Decorator_config_with_required_type()
        => Given("a decorator with type", () => new DecoratorConfig { Type = "logging" })
            .Then("type is set", decorator => decorator.Type == "logging")
            .And("type name is null", decorator => decorator.TypeName == null)
            .And("options is null", decorator => decorator.Options == null)
            .AssertPassed();

    [Scenario("Decorator config with all properties")]
    [Fact]
    public Task Decorator_config_with_all_properties()
        => Given("a custom decorator with all properties", () => new DecoratorConfig
            {
                Type = "custom",
                TypeName = "MyCustomDecorator",
                Options = new Dictionary<string, object> { ["key"] = "value" }
            })
            .Then("type is custom", decorator => decorator.Type == "custom")
            .And("type name is set", decorator => decorator.TypeName == "MyCustomDecorator")
            .And("options has one entry", decorator => decorator.Options!.Count == 1)
            .AssertPassed();

    #endregion

    #region TrialConfig Tests

    [Scenario("Trial config with required properties")]
    [Fact]
    public Task Trial_config_with_required_properties()
        => Given("a trial with required properties", () => new TrialConfig
            {
                ServiceType = "IMyService",
                SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                Control = new ConditionConfig { Key = "control", ImplementationType = "MyService" }
            })
            .Then("service type is set", trial => trial.ServiceType == "IMyService")
            .And("selection mode type is featureFlag", trial => trial.SelectionMode.Type == "featureFlag")
            .And("control key is set", trial => trial.Control.Key == "control")
            .And("conditions is null", trial => trial.Conditions == null)
            .And("error policy is null", trial => trial.ErrorPolicy == null)
            .And("activation is null", trial => trial.Activation == null)
            .AssertPassed();

    [Scenario("Trial config with optional properties")]
    [Fact]
    public Task Trial_config_with_optional_properties()
        => Given("a trial with all properties", () => new TrialConfig
            {
                ServiceType = "IMyService",
                SelectionMode = new SelectionModeConfig { Type = "featureFlag" },
                Control = new ConditionConfig { Key = "control", ImplementationType = "MyService" },
                Conditions = [new ConditionConfig { Key = "variant1", ImplementationType = "Variant1" }],
                ErrorPolicy = new ErrorPolicyConfig { Type = "fallbackToControl" },
                Activation = new ActivationConfig { From = DateTimeOffset.Now }
            })
            .Then("conditions has one item", trial => trial.Conditions!.Count == 1)
            .And("error policy type is set", trial => trial.ErrorPolicy!.Type == "fallbackToControl")
            .And("activation from is set", trial => trial.Activation!.From != null)
            .AssertPassed();

    #endregion

    #region SelectionModeConfig Tests

    [Scenario("Selection mode with required type")]
    [Fact]
    public Task SelectionMode_with_required_type()
        => Given("a selection mode with type", () => new SelectionModeConfig { Type = "featureFlag" })
            .Then("type is set", mode => mode.Type == "featureFlag")
            .And("flag name is null", mode => mode.FlagName == null)
            .And("key is null", mode => mode.Key == null)
            .And("flag key is null", mode => mode.FlagKey == null)
            .And("selector name is null", mode => mode.SelectorName == null)
            .And("mode identifier is null", mode => mode.ModeIdentifier == null)
            .AssertPassed();

    [Scenario("Selection mode for feature flag")]
    [Fact]
    public Task SelectionMode_for_feature_flag()
        => Given("a feature flag selection mode", () => new SelectionModeConfig
            {
                Type = "featureFlag",
                FlagName = "MyFlag"
            })
            .Then("type is featureFlag", mode => mode.Type == "featureFlag")
            .And("flag name is set", mode => mode.FlagName == "MyFlag")
            .AssertPassed();

    [Scenario("Selection mode for configuration key")]
    [Fact]
    public Task SelectionMode_for_configuration_key()
        => Given("a configuration key selection mode", () => new SelectionModeConfig
            {
                Type = "configurationKey",
                Key = "Settings:MyKey"
            })
            .Then("type is configurationKey", mode => mode.Type == "configurationKey")
            .And("key is set", mode => mode.Key == "Settings:MyKey")
            .AssertPassed();

    [Scenario("Selection mode for custom")]
    [Fact]
    public Task SelectionMode_for_custom()
        => Given("a custom selection mode", () => new SelectionModeConfig
            {
                Type = "custom",
                ModeIdentifier = "myCustomMode"
            })
            .Then("type is custom", mode => mode.Type == "custom")
            .And("mode identifier is set", mode => mode.ModeIdentifier == "myCustomMode")
            .AssertPassed();

    [Scenario("Selection mode for OpenFeature")]
    [Fact]
    public Task SelectionMode_for_openfeature()
        => Given("an OpenFeature selection mode", () => new SelectionModeConfig
            {
                Type = "openFeature",
                FlagKey = "my-flag"
            })
            .Then("type is openFeature", mode => mode.Type == "openFeature")
            .And("flag key is set", mode => mode.FlagKey == "my-flag")
            .AssertPassed();

    [Scenario("Selection mode for sticky routing")]
    [Fact]
    public Task SelectionMode_for_sticky_routing()
        => Given("a sticky routing selection mode", () => new SelectionModeConfig
            {
                Type = "stickyRouting",
                SelectorName = "myStickySelector"
            })
            .Then("type is stickyRouting", mode => mode.Type == "stickyRouting")
            .And("selector name is set", mode => mode.SelectorName == "myStickySelector")
            .AssertPassed();

    #endregion

    #region ConditionConfig Tests

    [Scenario("Condition config with required properties")]
    [Fact]
    public Task Condition_config_with_required_properties()
        => Given("a condition with required properties", () => new ConditionConfig
            {
                Key = "variant1",
                ImplementationType = "MyService"
            })
            .Then("key is set", condition => condition.Key == "variant1")
            .And("implementation type is set", condition => condition.ImplementationType == "MyService")
            .AssertPassed();

    [Scenario("Condition config with fully qualified type")]
    [Fact]
    public Task Condition_config_with_fully_qualified_type()
        => Given("a condition with assembly-qualified type", () => new ConditionConfig
            {
                Key = "variant1",
                ImplementationType = "MyService, MyAssembly"
            })
            .Then("implementation type includes assembly", condition => condition.ImplementationType == "MyService, MyAssembly")
            .AssertPassed();

    #endregion

    #region ErrorPolicyConfig Tests

    [Scenario("Error policy with required type")]
    [Fact]
    public Task ErrorPolicy_with_required_type()
        => Given("an error policy with type", () => new ErrorPolicyConfig { Type = "throw" })
            .Then("type is set", policy => policy.Type == "throw")
            .And("fallback key is null", policy => policy.FallbackKey == null)
            .And("fallback keys is null", policy => policy.FallbackKeys == null)
            .AssertPassed();

    [Scenario("Error policy with fallback to specific key")]
    [Fact]
    public Task ErrorPolicy_with_fallback_to_key()
        => Given("a fallbackTo error policy", () => new ErrorPolicyConfig
            {
                Type = "fallbackTo",
                FallbackKey = "control"
            })
            .Then("type is fallbackTo", policy => policy.Type == "fallbackTo")
            .And("fallback key is set", policy => policy.FallbackKey == "control")
            .AssertPassed();

    [Scenario("Error policy with try in order")]
    [Fact]
    public Task ErrorPolicy_with_try_in_order()
        => Given("a tryInOrder error policy", () => new ErrorPolicyConfig
            {
                Type = "tryInOrder",
                FallbackKeys = ["variant1", "variant2", "control"]
            })
            .Then("type is tryInOrder", policy => policy.Type == "tryInOrder")
            .And("fallback keys has three items", policy => policy.FallbackKeys!.Count == 3)
            .AssertPassed();

    #endregion

    #region ActivationConfig Tests

    [Scenario("Activation config has null defaults")]
    [Fact]
    public Task Activation_config_has_null_defaults()
        => Given("a new activation config", () => new ActivationConfig())
            .Then("from is null", activation => activation.From == null)
            .And("until is null", activation => activation.Until == null)
            .And("predicate is null", activation => activation.Predicate == null)
            .AssertPassed();

    [Scenario("Activation config with date range")]
    [Fact]
    public Task Activation_config_with_date_range()
        => Given("an activation with date range", () =>
            {
                var from = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
                var until = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero);
                return new ActivationConfig { From = from, Until = until };
            })
            .Then("from is set", activation => activation.From!.Value.Year == 2024)
            .And("until is set", activation => activation.Until!.Value.Year == 2024)
            .AssertPassed();

    [Scenario("Activation config with predicate")]
    [Fact]
    public Task Activation_config_with_predicate()
        => Given("an activation with predicate", () => new ActivationConfig
            {
                Predicate = new PredicateConfig { Type = "MyPredicate, MyAssembly" }
            })
            .Then("predicate is set", activation => activation.Predicate != null)
            .And("predicate type is set", activation => activation.Predicate!.Type == "MyPredicate, MyAssembly")
            .AssertPassed();

    #endregion

    #region ExperimentConfig Tests

    [Scenario("Experiment config with required properties")]
    [Fact]
    public Task Experiment_config_with_required_properties()
        => Given("an experiment with required properties", () => new ExperimentConfig
            {
                Name = "test-experiment",
                Trials = []
            })
            .Then("name is set", exp => exp.Name == "test-experiment")
            .And("trials is empty", exp => exp.Trials.Count == 0)
            .And("metadata is null", exp => exp.Metadata == null)
            .And("activation is null", exp => exp.Activation == null)
            .And("hypothesis is null", exp => exp.Hypothesis == null)
            .AssertPassed();

    [Scenario("Experiment config with optional properties")]
    [Fact]
    public Task Experiment_config_with_optional_properties()
        => Given("an experiment with all properties", () => new ExperimentConfig
            {
                Name = "test-experiment",
                Trials = [CreateValidTrialConfig()],
                Metadata = new Dictionary<string, object> { ["owner"] = "team-a" },
                Activation = new ActivationConfig { From = DateTimeOffset.Now },
                Hypothesis = CreateValidHypothesisConfig()
            })
            .Then("trials has one item", exp => exp.Trials.Count == 1)
            .And("metadata is set", exp => exp.Metadata!.Count == 1)
            .And("activation is set", exp => exp.Activation != null)
            .And("hypothesis is set", exp => exp.Hypothesis != null)
            .AssertPassed();

    #endregion

    #region HypothesisConfig Tests

    [Scenario("Hypothesis config with required properties")]
    [Fact]
    public Task Hypothesis_config_with_required_properties()
        => Given("a hypothesis with required properties", () => CreateValidHypothesisConfig())
            .Then("name is set", h => h.Name == "test-hypothesis")
            .And("type is superiority", h => h.Type == "superiority")
            .And("primary endpoint is set", h => h.PrimaryEndpoint != null)
            .And("expected effect size is set", h => Math.Abs(h.ExpectedEffectSize - 0.2) < 0.0001)
            .AssertPassed();

    [Scenario("Hypothesis config optional properties are null")]
    [Fact]
    public Task Hypothesis_config_optional_properties_are_null()
        => Given("a hypothesis with only required properties", () => CreateValidHypothesisConfig())
            .Then("description is null", h => h.Description == null)
            .And("secondary endpoints is null", h => h.SecondaryEndpoints == null)
            .And("control condition is null", h => h.ControlCondition == null)
            .And("treatment conditions is null", h => h.TreatmentConditions == null)
            .And("rationale is null", h => h.Rationale == null)
            .AssertPassed();

    #endregion

    #region EndpointConfig Tests

    [Scenario("Endpoint config with required properties")]
    [Fact]
    public Task Endpoint_config_with_required_properties()
        => Given("an endpoint with required properties", () => new EndpointConfig
            {
                Name = "response_time_ms",
                OutcomeType = "continuous"
            })
            .Then("name is set", e => e.Name == "response_time_ms")
            .And("outcome type is set", e => e.OutcomeType == "continuous")
            .AssertPassed();

    [Scenario("Endpoint config optional properties are null")]
    [Fact]
    public Task Endpoint_config_optional_properties_are_null()
        => Given("an endpoint with only required properties", () => new EndpointConfig
            {
                Name = "metric",
                OutcomeType = "continuous"
            })
            .Then("description is null", e => e.Description == null)
            .And("unit is null", e => e.Unit == null)
            .And("higher is better is null", e => e.HigherIsBetter == null)
            .And("lower is better is null", e => e.LowerIsBetter == null)
            .AssertPassed();

    #endregion

    #region SuccessCriteriaConfig Tests

    [Scenario("Success criteria has correct defaults")]
    [Fact]
    public Task SuccessCriteria_has_correct_defaults()
        => Given("a new success criteria config", () => new SuccessCriteriaConfig())
            .Then("alpha defaults to 0.05", c => Math.Abs(c.Alpha - 0.05) < 0.0001)
            .And("power defaults to 0.80", c => Math.Abs(c.Power - 0.80) <  0.0001)
            .And("primary endpoint only defaults to true", c => c.PrimaryEndpointOnly)
            .And("apply multiple comparison correction defaults to true", c => c.ApplyMultipleComparisonCorrection)
            .And("require positive effect defaults to true", c => c.RequirePositiveEffect)
            .AssertPassed();

    [Scenario("Success criteria optional properties are null")]
    [Fact]
    public Task SuccessCriteria_optional_properties_are_null()
        => Given("a new success criteria config", () => new SuccessCriteriaConfig())
            .Then("minimum sample size is null", c => c.MinimumSampleSize == null)
            .And("minimum effect size is null", c => c.MinimumEffectSize == null)
            .And("non-inferiority margin is null", c => c.NonInferiorityMargin == null)
            .And("equivalence margin is null", c => c.EquivalenceMargin == null)
            .And("minimum duration is null", c => c.MinimumDuration == null)
            .AssertPassed();

    #endregion

    #region Decorator Options Tests

    [Scenario("Logging decorator options defaults")]
    [Fact]
    public Task LoggingDecoratorOptions_defaults()
        => Given("new logging decorator options", () => new LoggingDecoratorOptions())
            .Then("benchmarks defaults to false", o => !o.Benchmarks)
            .And("error logging defaults to false", o => !o.ErrorLogging)
            .AssertPassed();

    [Scenario("Timeout decorator options defaults")]
    [Fact]
    public Task TimeoutDecoratorOptions_defaults()
        => Given("new timeout decorator options", () => new TimeoutDecoratorOptions())
            .Then("timeout defaults to 30 seconds", o => o.Timeout == TimeSpan.FromSeconds(30))
            .And("on timeout defaults to fallbackToDefault", o => o.OnTimeout == "fallbackToDefault")
            .And("fallback trial key is null", o => o.FallbackTrialKey == null)
            .AssertPassed();

    [Scenario("Circuit breaker options defaults")]
    [Fact]
    public Task CircuitBreakerOptions_defaults()
        => Given("new circuit breaker options", () => new CircuitBreakerDecoratorOptions())
            .Then("failure threshold defaults to 5", o => o.FailureThreshold == 5)
            .And("minimum throughput defaults to 10", o => o.MinimumThroughput == 10)
            .And("sampling duration defaults to 10 seconds", o => o.SamplingDuration == TimeSpan.FromSeconds(10))
            .And("break duration defaults to 30 seconds", o => o.BreakDuration == TimeSpan.FromSeconds(30))
            .And("on circuit open defaults to throw", o => o.OnCircuitOpen == "throw")
            .AssertPassed();

    [Scenario("Outcome collection options defaults")]
    [Fact]
    public Task OutcomeCollectionOptions_defaults()
        => Given("new outcome collection options", () => new OutcomeCollectionDecoratorOptions())
            .Then("auto generate ids defaults to true", o => o.AutoGenerateIds)
            .And("auto set timestamps defaults to true", o => o.AutoSetTimestamps)
            .And("collect duration defaults to true", o => o.CollectDuration)
            .And("collect errors defaults to true", o => o.CollectErrors)
            .And("enable batching defaults to false", o => !o.EnableBatching)
            .AssertPassed();

    #endregion

    #region PredicateConfig Tests

    [Scenario("Predicate config with required type")]
    [Fact]
    public Task PredicateConfig_with_required_type()
        => Given("a predicate config", () => new PredicateConfig { Type = "MyPredicate, MyAssembly" })
            .Then("type is set", p => p.Type == "MyPredicate, MyAssembly")
            .AssertPassed();

    #endregion
}
