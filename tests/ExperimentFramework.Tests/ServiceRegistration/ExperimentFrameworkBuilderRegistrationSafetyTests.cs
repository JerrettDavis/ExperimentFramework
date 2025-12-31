using ExperimentFramework.ServiceRegistration;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.ServiceRegistration;

[Feature("ExperimentFrameworkBuilder registration safety configuration")]
public class ExperimentFrameworkBuilderRegistrationSafetyTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Builder enables registration safety by default")]
    [Fact]
    public Task Builder_enables_safety_by_default()
        => Given("a new builder", () => ExperimentFrameworkBuilder.Create())
            .When("building configuration", builder => builder.Build())
            .Then("registration safety should be enabled", config => config.RegistrationSafetyEnabled)
            .And("validation mode should be Strict", config => config.RegistrationValidationMode == ValidationMode.Strict)
            .And("default behavior should be Replace", config => config.DefaultMultiRegistrationBehavior == MultiRegistrationBehavior.Replace)
            .And("emit report should be false", config => !config.EmitRegistrationReport)
            .AssertPassed();

    [Scenario("Builder can enable registration safety explicitly")]
    [Fact]
    public Task Builder_enables_safety_explicitly()
        => Given("a builder", () => ExperimentFrameworkBuilder.Create())
            .When("enabling registration safety", builder => builder.EnableRegistrationSafety().Build())
            .Then("registration safety should be enabled", config => config.RegistrationSafetyEnabled)
            .AssertPassed();

    [Scenario("Builder can disable registration safety")]
    [Fact]
    public Task Builder_disables_safety()
        => Given("a builder", () => ExperimentFrameworkBuilder.Create())
            .When("disabling registration safety", builder => builder.DisableRegistrationSafety().Build())
            .Then("registration safety should be disabled", config => !config.RegistrationSafetyEnabled)
            .AssertPassed();

    [Scenario("Builder can set validation mode to Strict")]
    [Fact]
    public Task Builder_sets_strict_validation()
        => Given("a builder", () => ExperimentFrameworkBuilder.Create())
            .When("setting validation mode to Strict", builder =>
                builder.WithRegistrationValidationMode(ValidationMode.Strict).Build())
            .Then("validation mode should be Strict", config => config.RegistrationValidationMode == ValidationMode.Strict)
            .AssertPassed();

    [Scenario("Builder can set validation mode to Warn")]
    [Fact]
    public Task Builder_sets_warn_validation()
        => Given("a builder", () => ExperimentFrameworkBuilder.Create())
            .When("setting validation mode to Warn", builder =>
                builder.WithRegistrationValidationMode(ValidationMode.Warn).Build())
            .Then("validation mode should be Warn", config => config.RegistrationValidationMode == ValidationMode.Warn)
            .AssertPassed();

    [Scenario("Builder can set validation mode to Off")]
    [Fact]
    public Task Builder_sets_off_validation()
        => Given("a builder", () => ExperimentFrameworkBuilder.Create())
            .When("setting validation mode to Off", builder =>
                builder.WithRegistrationValidationMode(ValidationMode.Off).Build())
            .Then("validation mode should be Off", config => config.RegistrationValidationMode == ValidationMode.Off)
            .AssertPassed();

    [Scenario("Builder can set multi-registration behavior to Replace")]
    [Fact]
    public Task Builder_sets_replace_behavior()
        => Given("a builder", () => ExperimentFrameworkBuilder.Create())
            .When("setting behavior to Replace", builder =>
                builder.WithDefaultMultiRegistrationBehavior(MultiRegistrationBehavior.Replace).Build())
            .Then("default behavior should be Replace", config => config.DefaultMultiRegistrationBehavior == MultiRegistrationBehavior.Replace)
            .AssertPassed();

    [Scenario("Builder can set multi-registration behavior to Insert")]
    [Fact]
    public Task Builder_sets_insert_behavior()
        => Given("a builder", () => ExperimentFrameworkBuilder.Create())
            .When("setting behavior to Insert", builder =>
                builder.WithDefaultMultiRegistrationBehavior(MultiRegistrationBehavior.Insert).Build())
            .Then("default behavior should be Insert", config => config.DefaultMultiRegistrationBehavior == MultiRegistrationBehavior.Insert)
            .AssertPassed();

    [Scenario("Builder can set multi-registration behavior to Append")]
    [Fact]
    public Task Builder_sets_append_behavior()
        => Given("a builder", () => ExperimentFrameworkBuilder.Create())
            .When("setting behavior to Append", builder =>
                builder.WithDefaultMultiRegistrationBehavior(MultiRegistrationBehavior.Append).Build())
            .Then("default behavior should be Append", config => config.DefaultMultiRegistrationBehavior == MultiRegistrationBehavior.Append)
            .AssertPassed();

    [Scenario("Builder can set multi-registration behavior to Merge")]
    [Fact]
    public Task Builder_sets_merge_behavior()
        => Given("a builder", () => ExperimentFrameworkBuilder.Create())
            .When("setting behavior to Merge", builder =>
                builder.WithDefaultMultiRegistrationBehavior(MultiRegistrationBehavior.Merge).Build())
            .Then("default behavior should be Merge", config => config.DefaultMultiRegistrationBehavior == MultiRegistrationBehavior.Merge)
            .AssertPassed();

    [Scenario("Builder can enable registration report emission")]
    [Fact]
    public Task Builder_enables_report_emission()
        => Given("a builder", () => ExperimentFrameworkBuilder.Create())
            .When("enabling report emission", builder => builder.EmitRegistrationReport().Build())
            .Then("emit report should be enabled", config => config.EmitRegistrationReport)
            .AssertPassed();

    [Scenario("Builder methods chain fluently")]
    [Fact]
    public Task Builder_methods_chain()
        => Given("a builder", () => ExperimentFrameworkBuilder.Create())
            .When("chaining multiple configuration methods", builder =>
                builder
                    .EnableRegistrationSafety()
                    .WithRegistrationValidationMode(ValidationMode.Warn)
                    .WithDefaultMultiRegistrationBehavior(MultiRegistrationBehavior.Insert)
                    .EmitRegistrationReport()
                    .Build())
            .Then("all settings should be applied", config =>
                config.RegistrationSafetyEnabled &&
                config.RegistrationValidationMode == ValidationMode.Warn &&
                config.DefaultMultiRegistrationBehavior == MultiRegistrationBehavior.Insert &&
                config.EmitRegistrationReport)
            .AssertPassed();

    [Scenario("Disabling safety overrides other settings")]
    [Fact]
    public Task Disabling_safety_is_honored()
        => Given("a builder with safety disabled", () => ExperimentFrameworkBuilder.Create())
            .When("disabling after other configurations", builder =>
                builder
                    .WithRegistrationValidationMode(ValidationMode.Strict)
                    .EmitRegistrationReport()
                    .DisableRegistrationSafety()
                    .Build())
            .Then("safety should be disabled", config => !config.RegistrationSafetyEnabled)
            .And("other settings should still be preserved", config =>
                config.RegistrationValidationMode == ValidationMode.Strict &&
                config.EmitRegistrationReport)
            .AssertPassed();
}
