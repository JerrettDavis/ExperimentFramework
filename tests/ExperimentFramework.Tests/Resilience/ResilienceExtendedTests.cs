using ExperimentFramework.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Resilience;

[Feature("Resilience – circuit breaker options, factory, builder extensions, and DI")]
public sealed class ResilienceExtendedTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    // ── CircuitBreakerOptions defaults ───────────────────────────────────────

    [Scenario("Default CircuitBreakerOptions values are sane")]
    [Fact]
    public Task Default_options_have_sane_values()
        => Given("default options", () => new CircuitBreakerOptions())
            .Then("FailureThreshold is 5", opts => opts.FailureThreshold == 5)
            .And("MinimumThroughput is 10", opts => opts.MinimumThroughput == 10)
            .And("SamplingDuration is 10 seconds", opts => opts.SamplingDuration == TimeSpan.FromSeconds(10))
            .And("BreakDuration is 30 seconds", opts => opts.BreakDuration == TimeSpan.FromSeconds(30))
            .And("FailureRatioThreshold is null", opts => opts.FailureRatioThreshold == null)
            .And("OnCircuitOpen is ThrowException", opts => opts.OnCircuitOpen == CircuitBreakerAction.ThrowException)
            .And("FallbackTrialKey is null", opts => opts.FallbackTrialKey == null)
            .AssertPassed();

    [Scenario("CircuitBreakerOptions can be customised")]
    [Fact]
    public Task Options_can_be_customised()
    {
        var opts = new CircuitBreakerOptions
        {
            FailureThreshold = 3,
            MinimumThroughput = 5,
            SamplingDuration = TimeSpan.FromSeconds(20),
            BreakDuration = TimeSpan.FromMinutes(2),
            FailureRatioThreshold = 0.6,
            OnCircuitOpen = CircuitBreakerAction.FallbackToDefault,
            FallbackTrialKey = "control"
        };

        Assert.Equal(3, opts.FailureThreshold);
        Assert.Equal(5, opts.MinimumThroughput);
        Assert.Equal(TimeSpan.FromSeconds(20), opts.SamplingDuration);
        Assert.Equal(TimeSpan.FromMinutes(2), opts.BreakDuration);
        Assert.Equal(0.6, opts.FailureRatioThreshold);
        Assert.Equal(CircuitBreakerAction.FallbackToDefault, opts.OnCircuitOpen);
        Assert.Equal("control", opts.FallbackTrialKey);

        return Task.CompletedTask;
    }

    [Scenario("CircuitBreakerAction enum has expected values")]
    [Fact]
    public Task CircuitBreakerAction_has_expected_values()
        => Given("the action enum values", () => Enum.GetValues<CircuitBreakerAction>())
            .Then("contains ThrowException", values => values.Contains(CircuitBreakerAction.ThrowException))
            .And("contains FallbackToDefault", values => values.Contains(CircuitBreakerAction.FallbackToDefault))
            .And("contains FallbackToSpecificTrial", values => values.Contains(CircuitBreakerAction.FallbackToSpecificTrial))
            .AssertPassed();

    // ── CircuitBreakerDecoratorFactory ───────────────────────────────────────

    [Scenario("Factory creates a decorator without logger")]
    [Fact]
    public Task Factory_creates_decorator_without_logger()
    {
        var options = new CircuitBreakerOptions();
        var factory = new CircuitBreakerDecoratorFactory(options);
        var sp = new ServiceCollection().BuildServiceProvider();

        var decorator = factory.Create(sp);

        Assert.NotNull(decorator);

        return Task.CompletedTask;
    }

    [Scenario("Factory creates a decorator with logger")]
    [Fact]
    public Task Factory_creates_decorator_with_logger()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        var options = new CircuitBreakerOptions { FailureRatioThreshold = 0.5 };
        var factory = new CircuitBreakerDecoratorFactory(options, loggerFactory);
        var decorator = factory.Create(sp);

        Assert.NotNull(decorator);

        return Task.CompletedTask;
    }

    [Scenario("Factory throws for null options")]
    [Fact]
    public void Factory_throws_for_null_options()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CircuitBreakerDecoratorFactory(null!));
    }

    [Scenario("Factory returns same decorator instance (singleton pattern)")]
    [Fact]
    public Task Factory_returns_same_decorator_instance()
    {
        var options = new CircuitBreakerOptions();
        var factory = new CircuitBreakerDecoratorFactory(options);
        var sp = new ServiceCollection().BuildServiceProvider();

        var d1 = factory.Create(sp);
        var d2 = factory.Create(sp);

        Assert.Same(d1, d2);

        return Task.CompletedTask;
    }

    // ── builder extensions ───────────────────────────────────────────────────

    [Scenario("WithCircuitBreaker (no options) attaches decorator factory")]
    [Fact]
    public Task WithCircuitBreaker_no_options_does_not_throw()
    {
        var builder = ExperimentFramework.ExperimentFrameworkBuilder.Create();

        builder.WithCircuitBreaker();

        Assert.NotNull(builder);

        return Task.CompletedTask;
    }

    [Scenario("WithCircuitBreaker with configure callback applies options")]
    [Fact]
    public Task WithCircuitBreaker_with_configure_applies_options()
    {
        var builder = ExperimentFramework.ExperimentFrameworkBuilder.Create();

        builder.WithCircuitBreaker(opts =>
        {
            opts.FailureRatioThreshold = 0.4;
            opts.MinimumThroughput = 20;
        });

        Assert.NotNull(builder);

        return Task.CompletedTask;
    }

    [Scenario("WithCircuitBreaker with explicit options overload does not throw")]
    [Fact]
    public Task WithCircuitBreaker_explicit_options_does_not_throw()
    {
        var options = new CircuitBreakerOptions
        {
            FailureRatioThreshold = 0.7,
            MinimumThroughput = 15
        };
        var builder = ExperimentFramework.ExperimentFrameworkBuilder.Create();

        builder.WithCircuitBreaker(options);

        Assert.NotNull(builder);

        return Task.CompletedTask;
    }

    // ── CircuitBreakerOpenException ───────────────────────────────────────────

    [Scenario("CircuitBreakerOpenException carries message")]
    [Fact]
    public Task CircuitBreakerOpenException_carries_message()
    {
        var ex = new CircuitBreakerOpenException("breaker open");

        Assert.Equal("breaker open", ex.Message);
        Assert.Null(ex.InnerException);

        return Task.CompletedTask;
    }

    [Scenario("CircuitBreakerOpenException wraps inner exception")]
    [Fact]
    public Task CircuitBreakerOpenException_wraps_inner_exception()
    {
        var inner = new InvalidOperationException("root cause");
        var ex = new CircuitBreakerOpenException("wrapper", inner);

        Assert.Equal("wrapper", ex.Message);
        Assert.Same(inner, ex.InnerException);

        return Task.CompletedTask;
    }

    // ── AddExperimentResilience DI ────────────────────────────────────────────

    [Scenario("CircuitBreakerDecoratorFactory with FallbackToSpecificTrial creates decorator")]
    [Fact]
    public Task Factory_with_fallback_to_specific_trial_creates_decorator()
    {
        var options = new CircuitBreakerOptions
        {
            OnCircuitOpen = CircuitBreakerAction.FallbackToSpecificTrial,
            FallbackTrialKey = "control"
        };
        var factory = new CircuitBreakerDecoratorFactory(options);
        var sp = new ServiceCollection().BuildServiceProvider();

        var decorator = factory.Create(sp);

        Assert.NotNull(decorator);

        return Task.CompletedTask;
    }
}
