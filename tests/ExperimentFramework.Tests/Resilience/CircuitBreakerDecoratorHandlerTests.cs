using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Resilience.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Resilience;

[Feature("CircuitBreakerDecoratorHandler handles configuration-based circuit breaker setup")]
public sealed class CircuitBreakerDecoratorHandlerTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Handler has correct decorator type")]
    [Fact]
    public Task Handler_has_correct_decorator_type()
        => Given("a handler", () => new CircuitBreakerDecoratorHandler())
            .Then("type is 'circuitBreaker'", h => h.DecoratorType == "circuitBreaker")
            .AssertPassed();

    [Scenario("Handler accepts logger factory")]
    [Fact]
    public Task Handler_accepts_logger_factory()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var loggerFactory = sp.GetService<ILoggerFactory>();

        return Given("a handler with logger factory", () => new CircuitBreakerDecoratorHandler(loggerFactory))
            .Then("handler is created", h => h != null)
            .AssertPassed();
    }

    [Scenario("Validate returns warning when options is null")]
    [Fact]
    public async Task Validate_returns_warning_when_options_null()
    {
        var handler = new CircuitBreakerDecoratorHandler();
        var config = new DecoratorConfig { Type = "circuitBreaker", Options = null };

        var errors = handler.Validate(config, "decorators[0]").ToList();

        Assert.Single(errors);
        Assert.Contains("warning", errors[0].Severity.ToString().ToLower());
        Assert.Contains("No circuit breaker options", errors[0].Message);

        await Task.CompletedTask;
    }

    [Scenario("Validate returns error for invalid failure ratio threshold")]
    [Fact]
    public async Task Validate_returns_error_for_invalid_failure_ratio()
    {
        var handler = new CircuitBreakerDecoratorHandler();
        var config = new DecoratorConfig
        {
            Type = "circuitBreaker",
            Options = new Dictionary<string, object>
            {
                ["failureRatioThreshold"] = 1.5 // Invalid: > 1
            }
        };

        var errors = handler.Validate(config, "decorators[0]").ToList();

        Assert.Single(errors);
        Assert.Contains("failureRatioThreshold", errors[0].Path);

        await Task.CompletedTask;
    }

    [Scenario("Validate returns error for zero failure ratio threshold")]
    [Fact]
    public async Task Validate_returns_error_for_zero_failure_ratio()
    {
        var handler = new CircuitBreakerDecoratorHandler();
        var config = new DecoratorConfig
        {
            Type = "circuitBreaker",
            Options = new Dictionary<string, object>
            {
                ["failureRatioThreshold"] = 0.0 // Invalid: <= 0
            }
        };

        var errors = handler.Validate(config, "decorators[0]").ToList();

        Assert.Single(errors);
        Assert.Contains("between 0", errors[0].Message);

        await Task.CompletedTask;
    }

    [Scenario("Validate returns error for invalid minimum throughput")]
    [Fact]
    public async Task Validate_returns_error_for_invalid_minimum_throughput()
    {
        var handler = new CircuitBreakerDecoratorHandler();
        var config = new DecoratorConfig
        {
            Type = "circuitBreaker",
            Options = new Dictionary<string, object>
            {
                ["minimumThroughput"] = 0 // Invalid: <= 0
            }
        };

        var errors = handler.Validate(config, "decorators[0]").ToList();

        Assert.Single(errors);
        Assert.Contains("minimumThroughput", errors[0].Path);

        await Task.CompletedTask;
    }

    [Scenario("Validate returns error for invalid sampling duration")]
    [Fact]
    public async Task Validate_returns_error_for_invalid_sampling_duration()
    {
        var handler = new CircuitBreakerDecoratorHandler();
        var config = new DecoratorConfig
        {
            Type = "circuitBreaker",
            Options = new Dictionary<string, object>
            {
                ["samplingDuration"] = "00:00:00" // Invalid: zero
            }
        };

        var errors = handler.Validate(config, "decorators[0]").ToList();

        Assert.Single(errors);
        Assert.Contains("samplingDuration", errors[0].Path);

        await Task.CompletedTask;
    }

    [Scenario("Validate returns error for invalid break duration")]
    [Fact]
    public async Task Validate_returns_error_for_invalid_break_duration()
    {
        var handler = new CircuitBreakerDecoratorHandler();
        var config = new DecoratorConfig
        {
            Type = "circuitBreaker",
            Options = new Dictionary<string, object>
            {
                ["breakDuration"] = "-00:00:01" // Invalid: negative
            }
        };

        var errors = handler.Validate(config, "decorators[0]").ToList();

        Assert.Single(errors);
        Assert.Contains("breakDuration", errors[0].Path);

        await Task.CompletedTask;
    }

    [Scenario("Validate passes for valid options")]
    [Fact]
    public async Task Validate_passes_for_valid_options()
    {
        var handler = new CircuitBreakerDecoratorHandler();
        var config = new DecoratorConfig
        {
            Type = "circuitBreaker",
            Options = new Dictionary<string, object>
            {
                ["failureRatioThreshold"] = 0.5,
                ["minimumThroughput"] = 10,
                ["samplingDuration"] = "00:00:30",
                ["breakDuration"] = "00:01:00"
            }
        };

        var errors = handler.Validate(config, "decorators[0]").ToList();

        Assert.Empty(errors);

        await Task.CompletedTask;
    }

    [Scenario("Apply adds circuit breaker to builder")]
    [Fact]
    public async Task Apply_adds_circuit_breaker_to_builder()
    {
        var handler = new CircuitBreakerDecoratorHandler();
        var builder = ExperimentFramework.ExperimentFrameworkBuilder.Create();
        var config = new DecoratorConfig
        {
            Type = "circuitBreaker",
            Options = new Dictionary<string, object>
            {
                ["failureRatioThreshold"] = 0.8,
                ["minimumThroughput"] = 5
            }
        };

        handler.Apply(builder, config, null);

        // The builder should have a decorator factory registered
        // We can verify by checking that the build doesn't throw
        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    [Scenario("Options parsing handles integer values")]
    [Fact]
    public async Task Options_parsing_handles_int_values()
    {
        var handler = new CircuitBreakerDecoratorHandler();
        var config = new DecoratorConfig
        {
            Type = "circuitBreaker",
            Options = new Dictionary<string, object>
            {
                ["failureRatioThreshold"] = 1, // int instead of double
                ["minimumThroughput"] = 10
            }
        };

        var errors = handler.Validate(config, "decorators[0]").ToList();

        Assert.Empty(errors);

        await Task.CompletedTask;
    }

    [Scenario("Options parsing handles long values")]
    [Fact]
    public async Task Options_parsing_handles_long_values()
    {
        var handler = new CircuitBreakerDecoratorHandler();
        var config = new DecoratorConfig
        {
            Type = "circuitBreaker",
            Options = new Dictionary<string, object>
            {
                ["minimumThroughput"] = 10L // long instead of int
            }
        };

        var errors = handler.Validate(config, "decorators[0]").ToList();

        Assert.Empty(errors);

        await Task.CompletedTask;
    }

    [Scenario("Options parsing handles string values")]
    [Fact]
    public async Task Options_parsing_handles_string_values()
    {
        var handler = new CircuitBreakerDecoratorHandler();
        var config = new DecoratorConfig
        {
            Type = "circuitBreaker",
            Options = new Dictionary<string, object>
            {
                ["failureRatioThreshold"] = "0.5",
                ["minimumThroughput"] = "10"
            }
        };

        var errors = handler.Validate(config, "decorators[0]").ToList();

        Assert.Empty(errors);

        await Task.CompletedTask;
    }

    [Scenario("Apply handles fallback trial key")]
    [Fact]
    public async Task Apply_handles_fallback_trial_key()
    {
        var handler = new CircuitBreakerDecoratorHandler();
        var builder = ExperimentFramework.ExperimentFrameworkBuilder.Create();
        var config = new DecoratorConfig
        {
            Type = "circuitBreaker",
            Options = new Dictionary<string, object>
            {
                ["fallbackTrialKey"] = "control"
            }
        };

        handler.Apply(builder, config, null);

        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    [Scenario("Apply handles null options")]
    [Fact]
    public async Task Apply_handles_null_options()
    {
        var handler = new CircuitBreakerDecoratorHandler();
        var builder = ExperimentFramework.ExperimentFrameworkBuilder.Create();
        var config = new DecoratorConfig
        {
            Type = "circuitBreaker",
            Options = null
        };

        handler.Apply(builder, config, null);

        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    [Scenario("Apply handles TimeSpan values in options")]
    [Fact]
    public async Task Apply_handles_timespan_values()
    {
        var handler = new CircuitBreakerDecoratorHandler();
        var builder = ExperimentFramework.ExperimentFrameworkBuilder.Create();
        var config = new DecoratorConfig
        {
            Type = "circuitBreaker",
            Options = new Dictionary<string, object>
            {
                ["samplingDuration"] = TimeSpan.FromSeconds(30),
                ["breakDuration"] = TimeSpan.FromMinutes(1)
            }
        };

        handler.Apply(builder, config, null);

        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    [Scenario("Apply ignores unsupported option types for double")]
    [Fact]
    public async Task Apply_ignores_unsupported_double_types()
    {
        var handler = new CircuitBreakerDecoratorHandler();
        var builder = ExperimentFramework.ExperimentFrameworkBuilder.Create();
        var config = new DecoratorConfig
        {
            Type = "circuitBreaker",
            Options = new Dictionary<string, object>
            {
                ["failureRatioThreshold"] = new object() // Unsupported type
            }
        };

        // Should not throw, just use default
        handler.Apply(builder, config, null);

        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    [Scenario("Apply ignores unsupported option types for int")]
    [Fact]
    public async Task Apply_ignores_unsupported_int_types()
    {
        var handler = new CircuitBreakerDecoratorHandler();
        var builder = ExperimentFramework.ExperimentFrameworkBuilder.Create();
        var config = new DecoratorConfig
        {
            Type = "circuitBreaker",
            Options = new Dictionary<string, object>
            {
                ["minimumThroughput"] = new object() // Unsupported type
            }
        };

        // Should not throw, just use default
        handler.Apply(builder, config, null);

        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    [Scenario("Apply ignores unsupported option types for TimeSpan")]
    [Fact]
    public async Task Apply_ignores_unsupported_timespan_types()
    {
        var handler = new CircuitBreakerDecoratorHandler();
        var builder = ExperimentFramework.ExperimentFrameworkBuilder.Create();
        var config = new DecoratorConfig
        {
            Type = "circuitBreaker",
            Options = new Dictionary<string, object>
            {
                ["samplingDuration"] = new object() // Unsupported type
            }
        };

        // Should not throw, just use default
        handler.Apply(builder, config, null);

        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    [Scenario("Options parsing handles long values for failureRatioThreshold")]
    [Fact]
    public async Task Options_parsing_handles_long_for_failure_ratio()
    {
        var handler = new CircuitBreakerDecoratorHandler();
        var config = new DecoratorConfig
        {
            Type = "circuitBreaker",
            Options = new Dictionary<string, object>
            {
                ["failureRatioThreshold"] = 1L // long value
            }
        };

        var errors = handler.Validate(config, "decorators[0]").ToList();

        Assert.Empty(errors);

        await Task.CompletedTask;
    }
}
