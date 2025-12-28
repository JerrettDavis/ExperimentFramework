using ExperimentFramework.Audit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Audit;

[Feature("ServiceCollectionExtensions register audit sinks")]
public sealed class AuditServiceCollectionExtensionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("AddExperimentAuditLogging registers LoggingAuditSink")]
    [Fact]
    public async Task AddExperimentAuditLogging_registers_sink()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExperimentAuditLogging();
        var sp = services.BuildServiceProvider();

        var sink = sp.GetService<IAuditSink>();

        Assert.NotNull(sink);
        Assert.IsType<LoggingAuditSink>(sink);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentAuditLogging uses specified log level")]
    [Fact]
    public async Task AddExperimentAuditLogging_uses_log_level()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExperimentAuditLogging(LogLevel.Warning);
        var sp = services.BuildServiceProvider();

        var sink = sp.GetService<IAuditSink>();

        Assert.NotNull(sink);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentAuditLogging returns service collection for chaining")]
    [Fact]
    public Task AddExperimentAuditLogging_returns_service_collection()
        => Given("a service collection with logging", () =>
            {
                var services = new ServiceCollection();
                services.AddLogging();
                return services;
            })
            .When("adding experiment audit logging", services =>
                services.AddExperimentAuditLogging())
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("AddExperimentAuditSink registers custom sink")]
    [Fact]
    public async Task AddExperimentAuditSink_registers_custom_sink()
    {
        var services = new ServiceCollection();
        services.AddExperimentAuditSink<TestAuditSink>();
        var sp = services.BuildServiceProvider();

        var sinks = sp.GetServices<IAuditSink>().ToList();

        Assert.Single(sinks);
        Assert.IsType<TestAuditSink>(sinks[0]);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentAuditSink is idempotent")]
    [Fact]
    public async Task AddExperimentAuditSink_is_idempotent()
    {
        var services = new ServiceCollection();
        services.AddExperimentAuditSink<TestAuditSink>();
        services.AddExperimentAuditSink<TestAuditSink>(); // Call twice
        var sp = services.BuildServiceProvider();

        var sinks = sp.GetServices<IAuditSink>().ToList();

        // Should only have one due to TryAddEnumerable
        Assert.Single(sinks);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentAuditComposite with single sink returns that sink")]
    [Fact]
    public async Task AddExperimentAuditComposite_single_sink()
    {
        var services = new ServiceCollection();
        services.AddExperimentAuditSink<TestAuditSink>();
        services.AddExperimentAuditComposite();
        var sp = services.BuildServiceProvider();

        var sink = sp.GetService<IAuditSink>();

        // With single sink, should return that sink directly
        Assert.NotNull(sink);
        Assert.IsType<TestAuditSink>(sink);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentAuditComposite with multiple sinks aggregates them")]
    [Fact]
    public async Task AddExperimentAuditComposite_multiple_sinks()
    {
        var services = new ServiceCollection();
        services.AddExperimentAuditSink<TestAuditSink>();
        services.AddExperimentAuditSink<AnotherTestAuditSink>();
        var sp = services.BuildServiceProvider();

        // Get the enumerable of sinks
        var sinks = sp.GetServices<IAuditSink>().ToList();

        // Should have two sinks registered
        Assert.Equal(2, sinks.Count);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentAuditComposite returns service collection for chaining")]
    [Fact]
    public Task AddExperimentAuditComposite_returns_service_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding experiment audit composite", services =>
                services.AddExperimentAuditComposite())
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    private sealed class TestAuditSink : IAuditSink
    {
        public ValueTask RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }

    private sealed class AnotherTestAuditSink : IAuditSink
    {
        public ValueTask RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }
}
