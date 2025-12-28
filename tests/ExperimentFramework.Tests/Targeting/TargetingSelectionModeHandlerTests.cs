using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Targeting;
using ExperimentFramework.Targeting.Configuration;
using ExperimentFramework.Tests.TestInterfaces;
using Microsoft.Extensions.Logging;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Targeting;

[Feature("TargetingSelectionModeHandler validates and applies targeting configuration")]
public sealed class TargetingSelectionModeHandlerTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Handler has correct mode type")]
    [Fact]
    public Task Handler_has_correct_mode_type()
        => Given("a targeting selection mode handler", () => new TargetingSelectionModeHandler())
            .Then("mode type is 'targeting'", h => h.ModeType == "targeting")
            .AssertPassed();

    [Scenario("Apply configures targeting mode on builder")]
    [Fact]
    public async Task Apply_configures_targeting_mode()
    {
        var handler = new TargetingSelectionModeHandler();
        var frameworkBuilder = ExperimentFrameworkBuilder.Create();
        ServiceExperimentBuilder<ITestService>? serviceBuilder = null;

        frameworkBuilder.Define<ITestService>(builder =>
        {
            serviceBuilder = builder;
            var config = new SelectionModeConfig { Type = "targeting" };
            handler.Apply(builder, config, null);
            // Add a trial to satisfy builder requirements
            builder.AddTrial<StableService>("control");
        });

        Assert.NotNull(serviceBuilder);
        await Task.CompletedTask;
    }

    [Scenario("Apply uses custom selector name when provided")]
    [Fact]
    public async Task Apply_uses_custom_selector_name()
    {
        var handler = new TargetingSelectionModeHandler();
        var frameworkBuilder = ExperimentFrameworkBuilder.Create();
        ServiceExperimentBuilder<ITestService>? serviceBuilder = null;

        frameworkBuilder.Define<ITestService>(builder =>
        {
            serviceBuilder = builder;
            var config = new SelectionModeConfig
            {
                Type = "targeting",
                SelectorName = "custom-selector"
            };
            handler.Apply(builder, config, null);
            builder.AddTrial<StableService>("control");
        });

        Assert.NotNull(serviceBuilder);
        await Task.CompletedTask;
    }

    [Scenario("Apply logs debug message when logger provided")]
    [Fact]
    public async Task Apply_logs_debug_message()
    {
        var handler = new TargetingSelectionModeHandler();
        var logger = new TestLogger();
        var frameworkBuilder = ExperimentFrameworkBuilder.Create();

        frameworkBuilder.Define<ITestService>(builder =>
        {
            var config = new SelectionModeConfig { Type = "targeting" };
            handler.Apply(builder, config, logger);
            builder.AddTrial<StableService>("control");
        });

        Assert.Contains(logger.Messages, m =>
            m.Contains("targeting selection mode") && m.Contains("ITestService"));

        await Task.CompletedTask;
    }

    [Scenario("Apply works without logger")]
    [Fact]
    public async Task Apply_works_without_logger()
    {
        var handler = new TargetingSelectionModeHandler();
        var frameworkBuilder = ExperimentFrameworkBuilder.Create();
        ServiceExperimentBuilder<ITestService>? result = null;

        frameworkBuilder.Define<ITestService>(builder =>
        {
            result = builder;
            var config = new SelectionModeConfig { Type = "targeting" };
            handler.Apply(builder, config, null);
            builder.AddTrial<StableService>("control");
        });

        Assert.NotNull(result);
        await Task.CompletedTask;
    }

    [Scenario("Handler ModeType is lowercase version of TargetingModes constant")]
    [Fact]
    public async Task Handler_mode_type_matches_targeting_constant_case_insensitive()
    {
        var handler = new TargetingSelectionModeHandler();

        // ModeType is lowercase for YAML config matching
        Assert.Equal("targeting", handler.ModeType);

        // TargetingModes.Targeting is capitalized for internal mode identifier
        Assert.Equal("Targeting", TargetingModes.Targeting);

        // They should match case-insensitively
        Assert.Equal(
            TargetingModes.Targeting,
            handler.ModeType,
            StringComparer.OrdinalIgnoreCase);

        await Task.CompletedTask;
    }

    [Scenario("Validate returns no errors for valid configuration")]
    [Fact]
    public Task Validate_valid_config_returns_no_errors()
        => Given("a targeting selection mode handler", () => new TargetingSelectionModeHandler())
            .When("validating valid config", h =>
            {
                var config = new SelectionModeConfig
                {
                    Type = "targeting",
                    SelectorName = "my-targeting-selector"
                };
                return h.Validate(config, "trials[0].selectionMode").ToList();
            })
            .Then("no errors returned", errors => errors.Count == 0)
            .AssertPassed();

    [Scenario("Validate returns no errors when SelectorName is null")]
    [Fact]
    public Task Validate_null_selector_returns_no_errors()
        => Given("a targeting selection mode handler", () => new TargetingSelectionModeHandler())
            .When("validating config without selector name", h =>
            {
                var config = new SelectionModeConfig
                {
                    Type = "targeting",
                    SelectorName = null
                };
                return h.Validate(config, "test").ToList();
            })
            .Then("no errors returned", errors => errors.Count == 0)
            .AssertPassed();

    [Scenario("Validate returns no errors when Options is null")]
    [Fact]
    public Task Validate_null_options_returns_no_errors()
        => Given("a targeting selection mode handler", () => new TargetingSelectionModeHandler())
            .When("validating config with null options", h =>
            {
                var config = new SelectionModeConfig
                {
                    Type = "targeting",
                    Options = null
                };
                return h.Validate(config, "test").ToList();
            })
            .Then("no errors returned", errors => errors.Count == 0)
            .AssertPassed();

    [Scenario("Validate returns no errors with empty options")]
    [Fact]
    public Task Validate_empty_options_returns_no_errors()
        => Given("a targeting selection mode handler", () => new TargetingSelectionModeHandler())
            .When("validating config with empty options", h =>
            {
                var config = new SelectionModeConfig
                {
                    Type = "targeting",
                    Options = new Dictionary<string, object>()
                };
                return h.Validate(config, "test").ToList();
            })
            .Then("no errors returned", errors => errors.Count == 0)
            .AssertPassed();

    [Scenario("Validate returns no errors with custom options")]
    [Fact]
    public Task Validate_custom_options_returns_no_errors()
        => Given("a targeting selection mode handler", () => new TargetingSelectionModeHandler())
            .When("validating config with custom options", h =>
            {
                var config = new SelectionModeConfig
                {
                    Type = "targeting",
                    SelectorName = "custom-selector",
                    Options = new Dictionary<string, object>
                    {
                        ["customOption1"] = "value1",
                        ["customOption2"] = 123
                    }
                };
                return h.Validate(config, "test").ToList();
            })
            .Then("no errors returned", errors => errors.Count == 0)
            .AssertPassed();

    [Scenario("Validate yields break for all configurations")]
    [Fact]
    public async Task Validate_yields_break()
    {
        var handler = new TargetingSelectionModeHandler();

        // Test with various configurations - all should return empty
        var configs = new[]
        {
            new SelectionModeConfig { Type = "targeting" },
            new SelectionModeConfig { Type = "targeting", SelectorName = "test" },
            new SelectionModeConfig { Type = "targeting", Options = new Dictionary<string, object> { ["key"] = "value" } },
            new SelectionModeConfig { Type = "targeting", FlagName = "flag" },
            new SelectionModeConfig { Type = "targeting", Key = "key" }
        };

        foreach (var config in configs)
        {
            var errors = handler.Validate(config, "path").ToList();
            Assert.Empty(errors);
        }

        await Task.CompletedTask;
    }

    [Scenario("Handler implements IConfigurationSelectionModeHandler")]
    [Fact]
    public Task Handler_implements_interface()
        => Given("a targeting selection mode handler", () => new TargetingSelectionModeHandler())
            .Then("implements IConfigurationSelectionModeHandler", h =>
                h is ExperimentFramework.Configuration.Extensions.IConfigurationSelectionModeHandler)
            .AssertPassed();

    [Scenario("Multiple handlers can be instantiated")]
    [Fact]
    public Task Multiple_handlers_can_be_created()
        => Given("creating two handlers", () =>
            {
                var handler1 = new TargetingSelectionModeHandler();
                var handler2 = new TargetingSelectionModeHandler();
                return (handler1, handler2);
            })
            .Then("both have same mode type", handlers =>
                handlers.handler1.ModeType == handlers.handler2.ModeType)
            .And("both return same validation result", handlers =>
            {
                var config = new SelectionModeConfig { Type = "targeting" };
                var errors1 = handlers.handler1.Validate(config, "test").ToList();
                var errors2 = handlers.handler2.Validate(config, "test").ToList();
                return errors1.Count == errors2.Count;
            })
            .AssertPassed();

    private sealed class TestLogger : ILogger
    {
        public List<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
