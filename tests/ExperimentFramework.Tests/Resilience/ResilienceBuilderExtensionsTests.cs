using ExperimentFramework.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Resilience;

[Feature("ResilienceBuilderExtensions provides circuit breaker extension methods")]
public sealed class ResilienceBuilderExtensionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("WithCircuitBreaker adds default circuit breaker")]
    [Fact]
    public Task WithCircuitBreaker_adds_default_circuit_breaker()
        => Given("a builder", () => ExperimentFramework.ExperimentFrameworkBuilder.Create())
            .When("adding default circuit breaker", b => b.WithCircuitBreaker())
            .Then("returns the builder", b => b != null)
            .AssertPassed();

    [Scenario("WithCircuitBreaker accepts configuration action")]
    [Fact]
    public Task WithCircuitBreaker_accepts_configuration_action()
        => Given("a builder", () => ExperimentFramework.ExperimentFrameworkBuilder.Create())
            .When("adding configured circuit breaker", b => b.WithCircuitBreaker(opts =>
            {
                opts.MinimumThroughput = 20;
                opts.BreakDuration = TimeSpan.FromSeconds(30);
            }))
            .Then("returns the builder", b => b != null)
            .AssertPassed();

    [Scenario("WithCircuitBreaker accepts null configuration action")]
    [Fact]
    public Task WithCircuitBreaker_accepts_null_configuration_action()
        => Given("a builder", () => ExperimentFramework.ExperimentFrameworkBuilder.Create())
            .When("adding circuit breaker with null action", b => b.WithCircuitBreaker((Action<CircuitBreakerOptions>?)null))
            .Then("returns the builder", b => b != null)
            .AssertPassed();

    [Scenario("WithCircuitBreaker accepts logger factory")]
    [Fact]
    public async Task WithCircuitBreaker_accepts_logger_factory()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var loggerFactory = sp.GetService<ILoggerFactory>();

        var builder = ExperimentFramework.ExperimentFrameworkBuilder.Create();
        var result = builder.WithCircuitBreaker(opts =>
        {
            opts.MinimumThroughput = 5;
        }, loggerFactory);

        Assert.NotNull(result);

        await Task.CompletedTask;
    }

    [Scenario("WithCircuitBreaker overload accepts options directly")]
    [Fact]
    public Task WithCircuitBreaker_overload_accepts_options()
    {
        var options = new CircuitBreakerOptions
        {
            MinimumThroughput = 15,
            FailureRatioThreshold = 0.6,
            SamplingDuration = TimeSpan.FromMinutes(1),
            BreakDuration = TimeSpan.FromMinutes(2)
        };

        return Given("a builder and options", () => (
            Builder: ExperimentFramework.ExperimentFrameworkBuilder.Create(),
            Options: options))
            .When("adding circuit breaker with options", data => data.Builder.WithCircuitBreaker(data.Options))
            .Then("returns the builder", b => b != null)
            .AssertPassed();
    }

    [Scenario("WithCircuitBreaker options overload accepts logger factory")]
    [Fact]
    public async Task WithCircuitBreaker_options_overload_accepts_logger_factory()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var loggerFactory = sp.GetService<ILoggerFactory>();

        var options = new CircuitBreakerOptions
        {
            MinimumThroughput = 10
        };

        var builder = ExperimentFramework.ExperimentFrameworkBuilder.Create();
        var result = builder.WithCircuitBreaker(options, loggerFactory);

        Assert.NotNull(result);

        await Task.CompletedTask;
    }

    [Scenario("Multiple circuit breakers can be chained")]
    [Fact]
    public Task Multiple_circuit_breakers_can_be_chained()
        => Given("a builder", () => ExperimentFramework.ExperimentFrameworkBuilder.Create())
            .When("adding multiple circuit breakers", b => b
                .WithCircuitBreaker(opts => opts.MinimumThroughput = 5)
                .WithCircuitBreaker(opts => opts.MinimumThroughput = 10))
            .Then("returns the builder", b => b != null)
            .AssertPassed();
}
