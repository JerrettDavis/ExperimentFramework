using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using ExperimentFramework.Data.Configuration;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Data;

[Feature("OutcomeCollectionDecoratorHandler configures outcome collection from config")]
public sealed class OutcomeCollectionDecoratorHandlerTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("DecoratorType returns 'outcomeCollection'")]
    [Fact]
    public Task DecoratorType_returns_outcomeCollection()
        => Given("an OutcomeCollectionDecoratorHandler", () => new OutcomeCollectionDecoratorHandler())
            .Then("DecoratorType is 'outcomeCollection'", h => h.DecoratorType == "outcomeCollection")
            .AssertPassed();

    [Scenario("Validate with null options returns no errors")]
    [Fact]
    public Task Validate_null_options_returns_no_errors()
        => Given("a handler and config with null options", () => (
                Handler: new OutcomeCollectionDecoratorHandler(),
                Config: new DecoratorConfig { Type = "outcomeCollection", Options = null }))
            .When("validating", ctx => ctx.Handler.Validate(ctx.Config, "decorators[0]").ToList())
            .Then("returns no errors", errors => errors.Count == 0)
            .AssertPassed();

    [Scenario("Validate with empty options returns no errors")]
    [Fact]
    public Task Validate_empty_options_returns_no_errors()
        => Given("a handler and config with empty options", () => (
                Handler: new OutcomeCollectionDecoratorHandler(),
                Config: new DecoratorConfig
                {
                    Type = "outcomeCollection",
                    Options = new Dictionary<string, object>()
                }))
            .When("validating", ctx => ctx.Handler.Validate(ctx.Config, "decorators[0]").ToList())
            .Then("returns no errors", errors => errors.Count == 0)
            .AssertPassed();

    [Scenario("Validate with valid options returns no errors")]
    [Fact]
    public Task Validate_valid_options_returns_no_errors()
        => Given("a handler and config with valid options", () => (
                Handler: new OutcomeCollectionDecoratorHandler(),
                Config: new DecoratorConfig
                {
                    Type = "outcomeCollection",
                    Options = new Dictionary<string, object>
                    {
                        ["autoGenerateIds"] = true,
                        ["collectDuration"] = true,
                        ["maxBatchSize"] = 100,
                        ["batchFlushInterval"] = "00:00:05"
                    }
                }))
            .When("validating", ctx => ctx.Handler.Validate(ctx.Config, "decorators[0]").ToList())
            .Then("returns no errors", errors => errors.Count == 0)
            .AssertPassed();

    [Scenario("Validate with zero maxBatchSize returns error")]
    [Fact]
    public Task Validate_zero_maxBatchSize_returns_error()
        => Given("a handler and config with zero maxBatchSize", () => (
                Handler: new OutcomeCollectionDecoratorHandler(),
                Config: new DecoratorConfig
                {
                    Type = "outcomeCollection",
                    Options = new Dictionary<string, object>
                    {
                        ["maxBatchSize"] = 0
                    }
                }))
            .When("validating", ctx => ctx.Handler.Validate(ctx.Config, "decorators[0]").ToList())
            .Then("returns an error", errors => errors.Count == 1)
            .And("error is for maxBatchSize", errors =>
                errors[0].Path.Contains("maxBatchSize") &&
                errors[0].Message.Contains("positive"))
            .AssertPassed();

    [Scenario("Validate with negative maxBatchSize returns error")]
    [Fact]
    public Task Validate_negative_maxBatchSize_returns_error()
        => Given("a handler and config with negative maxBatchSize", () => (
                Handler: new OutcomeCollectionDecoratorHandler(),
                Config: new DecoratorConfig
                {
                    Type = "outcomeCollection",
                    Options = new Dictionary<string, object>
                    {
                        ["maxBatchSize"] = -1
                    }
                }))
            .When("validating", ctx => ctx.Handler.Validate(ctx.Config, "decorators[0]").ToList())
            .Then("returns an error", errors => errors.Count == 1)
            .And("error severity is Error", errors => errors[0].Severity == ValidationSeverity.Error)
            .AssertPassed();

    [Scenario("Validate with zero batchFlushInterval returns error")]
    [Fact]
    public Task Validate_zero_batchFlushInterval_returns_error()
        => Given("a handler and config with zero interval", () => (
                Handler: new OutcomeCollectionDecoratorHandler(),
                Config: new DecoratorConfig
                {
                    Type = "outcomeCollection",
                    Options = new Dictionary<string, object>
                    {
                        ["batchFlushInterval"] = "00:00:00"
                    }
                }))
            .When("validating", ctx => ctx.Handler.Validate(ctx.Config, "decorators[0]").ToList())
            .Then("returns an error", errors => errors.Count == 1)
            .And("error is for batchFlushInterval", errors =>
                errors[0].Path.Contains("batchFlushInterval") &&
                errors[0].Message.Contains("positive"))
            .AssertPassed();

    [Scenario("Validate with negative batchFlushInterval returns error")]
    [Fact]
    public Task Validate_negative_batchFlushInterval_returns_error()
        => Given("a handler and config with negative interval", () => (
                Handler: new OutcomeCollectionDecoratorHandler(),
                Config: new DecoratorConfig
                {
                    Type = "outcomeCollection",
                    Options = new Dictionary<string, object>
                    {
                        ["batchFlushInterval"] = "-00:00:01"
                    }
                }))
            .When("validating", ctx => ctx.Handler.Validate(ctx.Config, "decorators[0]").ToList())
            .Then("returns an error", errors => errors.Count == 1)
            .And("error severity is Error", errors => errors[0].Severity == ValidationSeverity.Error)
            .AssertPassed();

    [Scenario("Apply with null options uses defaults")]
    [Fact]
    public async Task Apply_with_null_options_uses_defaults()
    {
        // Arrange
        var handler = new OutcomeCollectionDecoratorHandler();
        var config = new DecoratorConfig { Type = "outcomeCollection", Options = null };
        var builder = ExperimentFrameworkBuilder.Create();

        // Act
        handler.Apply(builder, config, null);

        // Assert - Should not throw and builder should have a decorator factory
        var factories = builder.GetDecoratorFactories();
        Assert.NotEmpty(factories);
    }

    [Scenario("Apply with all options configures properly")]
    [Fact]
    public async Task Apply_with_all_options()
    {
        // Arrange
        var handler = new OutcomeCollectionDecoratorHandler();
        var config = new DecoratorConfig
        {
            Type = "outcomeCollection",
            Options = new Dictionary<string, object>
            {
                ["autoGenerateIds"] = false,
                ["autoSetTimestamps"] = false,
                ["collectDuration"] = false,
                ["collectErrors"] = false,
                ["durationMetricName"] = "custom_duration",
                ["errorMetricName"] = "custom_error",
                ["successMetricName"] = "custom_success",
                ["enableBatching"] = true,
                ["maxBatchSize"] = 50,
                ["batchFlushInterval"] = "00:00:10"
            }
        };
        var builder = ExperimentFrameworkBuilder.Create();

        // Act
        handler.Apply(builder, config, null);

        // Assert
        var factories = builder.GetDecoratorFactories();
        Assert.NotEmpty(factories);
    }

    [Scenario("Apply with boolean as string")]
    [Fact]
    public async Task Apply_with_boolean_as_string()
    {
        // Arrange
        var handler = new OutcomeCollectionDecoratorHandler();
        var config = new DecoratorConfig
        {
            Type = "outcomeCollection",
            Options = new Dictionary<string, object>
            {
                ["autoGenerateIds"] = "true",
                ["collectDuration"] = "false"
            }
        };
        var builder = ExperimentFrameworkBuilder.Create();

        // Act
        handler.Apply(builder, config, null);

        // Assert - Should not throw
        var factories = builder.GetDecoratorFactories();
        Assert.NotEmpty(factories);
    }

    [Scenario("Apply with integer as long")]
    [Fact]
    public async Task Apply_with_integer_as_long()
    {
        // Arrange
        var handler = new OutcomeCollectionDecoratorHandler();
        var config = new DecoratorConfig
        {
            Type = "outcomeCollection",
            Options = new Dictionary<string, object>
            {
                ["maxBatchSize"] = 100L // long value
            }
        };
        var builder = ExperimentFrameworkBuilder.Create();

        // Act
        handler.Apply(builder, config, null);

        // Assert - Should not throw
        var factories = builder.GetDecoratorFactories();
        Assert.NotEmpty(factories);
    }

    [Scenario("Apply with integer as string")]
    [Fact]
    public async Task Apply_with_integer_as_string()
    {
        // Arrange
        var handler = new OutcomeCollectionDecoratorHandler();
        var config = new DecoratorConfig
        {
            Type = "outcomeCollection",
            Options = new Dictionary<string, object>
            {
                ["maxBatchSize"] = "200"
            }
        };
        var builder = ExperimentFrameworkBuilder.Create();

        // Act
        handler.Apply(builder, config, null);

        // Assert - Should not throw
        var factories = builder.GetDecoratorFactories();
        Assert.NotEmpty(factories);
    }

    [Scenario("Apply with timespan as TimeSpan object")]
    [Fact]
    public async Task Apply_with_timespan_as_object()
    {
        // Arrange
        var handler = new OutcomeCollectionDecoratorHandler();
        var config = new DecoratorConfig
        {
            Type = "outcomeCollection",
            Options = new Dictionary<string, object>
            {
                ["batchFlushInterval"] = TimeSpan.FromSeconds(15)
            }
        };
        var builder = ExperimentFrameworkBuilder.Create();

        // Act
        handler.Apply(builder, config, null);

        // Assert - Should not throw
        var factories = builder.GetDecoratorFactories();
        Assert.NotEmpty(factories);
    }

    [Scenario("Error path includes decorator index")]
    [Fact]
    public Task Error_path_includes_decorator_index()
        => Given("a handler and invalid config", () => (
                Handler: new OutcomeCollectionDecoratorHandler(),
                Config: new DecoratorConfig
                {
                    Type = "outcomeCollection",
                    Options = new Dictionary<string, object> { ["maxBatchSize"] = -1 }
                }))
            .When("validating with specific path", ctx =>
                ctx.Handler.Validate(ctx.Config, "experimentFramework.decorators[2]").ToList())
            .Then("error path includes the provided path", errors =>
                errors[0].Path.StartsWith("experimentFramework.decorators[2]"))
            .AssertPassed();

    [Scenario("Multiple validation errors can be returned")]
    [Fact]
    public Task Multiple_validation_errors()
        => Given("a handler and config with multiple errors", () => (
                Handler: new OutcomeCollectionDecoratorHandler(),
                Config: new DecoratorConfig
                {
                    Type = "outcomeCollection",
                    Options = new Dictionary<string, object>
                    {
                        ["maxBatchSize"] = 0,
                        ["batchFlushInterval"] = "00:00:00"
                    }
                }))
            .When("validating", ctx => ctx.Handler.Validate(ctx.Config, "decorators[0]").ToList())
            .Then("returns two errors", errors => errors.Count == 2)
            .AssertPassed();

    [Scenario("Unknown options are ignored")]
    [Fact]
    public async Task Unknown_options_are_ignored()
    {
        // Arrange
        var handler = new OutcomeCollectionDecoratorHandler();
        var config = new DecoratorConfig
        {
            Type = "outcomeCollection",
            Options = new Dictionary<string, object>
            {
                ["unknownOption"] = "value",
                ["anotherUnknown"] = 123
            }
        };
        var builder = ExperimentFrameworkBuilder.Create();

        // Act - Should not throw
        handler.Apply(builder, config, null);
        var errors = handler.Validate(config, "decorators[0]").ToList();

        // Assert
        Assert.Empty(errors);
    }

    [Scenario("Case sensitivity in option keys")]
    [Fact]
    public async Task Case_sensitivity_in_option_keys()
    {
        // Arrange
        var handler = new OutcomeCollectionDecoratorHandler();
        var config = new DecoratorConfig
        {
            Type = "outcomeCollection",
            Options = new Dictionary<string, object>
            {
                // Note: These are case-sensitive and may not match
                ["AutoGenerateIds"] = false, // Wrong case
                ["autogenerateids"] = false  // Wrong case
            }
        };
        var builder = ExperimentFrameworkBuilder.Create();

        // Act - Should not throw even with wrong case keys
        handler.Apply(builder, config, null);

        // Assert
        var factories = builder.GetDecoratorFactories();
        Assert.NotEmpty(factories);
    }

    [Scenario("Apply ignores unsupported bool type")]
    [Fact]
    public async Task Apply_ignores_unsupported_bool_type()
    {
        // Arrange
        var handler = new OutcomeCollectionDecoratorHandler();
        var config = new DecoratorConfig
        {
            Type = "outcomeCollection",
            Options = new Dictionary<string, object>
            {
                ["autoGenerateIds"] = new object() // Unsupported type
            }
        };
        var builder = ExperimentFrameworkBuilder.Create();

        // Act - Should not throw, just use default
        handler.Apply(builder, config, null);

        // Assert
        var factories = builder.GetDecoratorFactories();
        Assert.NotEmpty(factories);
    }

    [Scenario("Apply ignores unsupported int type")]
    [Fact]
    public async Task Apply_ignores_unsupported_int_type()
    {
        // Arrange
        var handler = new OutcomeCollectionDecoratorHandler();
        var config = new DecoratorConfig
        {
            Type = "outcomeCollection",
            Options = new Dictionary<string, object>
            {
                ["maxBatchSize"] = new object() // Unsupported type
            }
        };
        var builder = ExperimentFrameworkBuilder.Create();

        // Act - Should not throw, just use default
        handler.Apply(builder, config, null);

        // Assert
        var factories = builder.GetDecoratorFactories();
        Assert.NotEmpty(factories);
    }

    [Scenario("Apply ignores unsupported timespan type")]
    [Fact]
    public async Task Apply_ignores_unsupported_timespan_type()
    {
        // Arrange
        var handler = new OutcomeCollectionDecoratorHandler();
        var config = new DecoratorConfig
        {
            Type = "outcomeCollection",
            Options = new Dictionary<string, object>
            {
                ["batchFlushInterval"] = new object() // Unsupported type
            }
        };
        var builder = ExperimentFrameworkBuilder.Create();

        // Act - Should not throw, just use default
        handler.Apply(builder, config, null);

        // Assert
        var factories = builder.GetDecoratorFactories();
        Assert.NotEmpty(factories);
    }
}
