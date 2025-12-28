using ExperimentFramework.Decorators;
using ExperimentFramework.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Resilience;

[Feature("CircuitBreakerDecoratorFactory creates circuit breaker decorators")]
public sealed class CircuitBreakerDecoratorFactoryTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Factory throws when options is null")]
    [Fact]
    public Task Factory_throws_when_options_null()
        => Given("null options", () => (CircuitBreakerOptions?)null)
            .When("creating factory", opts =>
            {
                try
                {
                    _ = new CircuitBreakerDecoratorFactory(opts!);
                    return false;
                }
                catch (ArgumentNullException)
                {
                    return true;
                }
            })
            .Then("throws ArgumentNullException", threw => threw)
            .AssertPassed();

    [Scenario("Factory creates decorator successfully")]
    [Fact]
    public Task Factory_creates_decorator()
        => Given("circuit breaker options", () => new CircuitBreakerOptions())
            .When("creating factory and getting decorator", opts =>
            {
                var factory = new CircuitBreakerDecoratorFactory(opts);
                var sp = new ServiceCollection().BuildServiceProvider();
                return factory.Create(sp);
            })
            .Then("decorator is not null", decorator => decorator != null)
            .AssertPassed();

    [Scenario("Factory returns singleton decorator instance")]
    [Fact]
    public async Task Factory_returns_singleton_decorator()
    {
        var options = new CircuitBreakerOptions();
        var factory = new CircuitBreakerDecoratorFactory(options);
        var sp = new ServiceCollection().BuildServiceProvider();

        var decorator1 = factory.Create(sp);
        var decorator2 = factory.Create(sp);

        Assert.Same(decorator1, decorator2);

        await Task.CompletedTask;
    }

    [Scenario("Factory accepts logger factory")]
    [Fact]
    public async Task Factory_accepts_logger_factory()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var loggerFactory = sp.GetService<ILoggerFactory>();

        var options = new CircuitBreakerOptions();
        var factory = new CircuitBreakerDecoratorFactory(options, loggerFactory);
        var decorator = factory.Create(sp);

        Assert.NotNull(decorator);

        await Task.CompletedTask;
    }

    [Scenario("Decorator can invoke successful operations")]
    [Fact]
    public async Task Decorator_invokes_successful_operations()
    {
        var options = new CircuitBreakerOptions
        {
            MinimumThroughput = 2, // Polly requires minimum of 2
            FailureRatioThreshold = 0.9
        };
        var factory = new CircuitBreakerDecoratorFactory(options);
        var sp = new ServiceCollection().BuildServiceProvider();
        var decorator = factory.Create(sp);

        var context = new InvocationContext(
            typeof(IFormattable),
            "ToString",
            "test",
            []);

        var result = await decorator.InvokeAsync(context, async () =>
        {
            await Task.Yield();
            return "success";
        });

        Assert.Equal("success", result);
    }

    [Scenario("Decorator throws CircuitBreakerOpenException when circuit opens")]
    [Fact]
    public async Task Decorator_throws_when_circuit_opens()
    {
        var options = new CircuitBreakerOptions
        {
            MinimumThroughput = 2,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromMinutes(1),
            FailureRatioThreshold = 0.5 // 50% failure rate
        };
        var factory = new CircuitBreakerDecoratorFactory(options);
        var sp = new ServiceCollection().BuildServiceProvider();
        var decorator = factory.Create(sp);

        var context = new InvocationContext(
            typeof(IFormattable),
            "ToString",
            "test",
            []);

        // Force failures to open the circuit
        for (int i = 0; i < 10; i++)
        {
            try
            {
                await decorator.InvokeAsync(context, () =>
                    throw new InvalidOperationException("Simulated failure"));
            }
            catch (InvalidOperationException)
            {
                // Expected
            }
            catch (CircuitBreakerOpenException)
            {
                // Circuit opened - test passed
                return;
            }
        }

        // If we got here without a CircuitBreakerOpenException, the circuit should be open now
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () =>
        {
            await decorator.InvokeAsync(context, () =>
                ValueTask.FromResult<object?>("success"));
        });
    }
}
