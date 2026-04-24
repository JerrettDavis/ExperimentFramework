using ExperimentFramework.Audit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Audit.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddExperimentAuditLogging_RegistersLoggingAuditSink()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExperimentAuditLogging();

        using var sp = services.BuildServiceProvider();
        var sink = sp.GetService<IAuditSink>();

        Assert.NotNull(sink);
        Assert.IsType<LoggingAuditSink>(sink);
    }

    [Fact]
    public void AddExperimentAuditLogging_WithCustomLogLevel_RegistersSink()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExperimentAuditLogging(LogLevel.Warning);

        using var sp = services.BuildServiceProvider();
        var sink = sp.GetRequiredService<IAuditSink>();

        Assert.IsType<LoggingAuditSink>(sink);
    }

    [Fact]
    public void AddExperimentAuditLogging_CalledTwice_RegistersOnce()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExperimentAuditLogging();
        services.AddExperimentAuditLogging(); // second call should be a no-op

        using var sp = services.BuildServiceProvider();
        var sinks = sp.GetServices<IAuditSink>().ToList();

        // TryAddSingleton only registers once
        Assert.Single(sinks);
    }

    [Fact]
    public void AddExperimentAuditSink_RegistersCustomSink()
    {
        var services = new ServiceCollection();
        services.AddExperimentAuditSink<NoopAuditSink>();

        using var sp = services.BuildServiceProvider();
        var sinks = sp.GetServices<IAuditSink>().ToList();

        Assert.Single(sinks);
        Assert.IsType<NoopAuditSink>(sinks[0]);
    }

    [Fact]
    public void AddExperimentAuditSink_CalledTwiceSameType_RegistersOnce()
    {
        var services = new ServiceCollection();
        services.AddExperimentAuditSink<NoopAuditSink>();
        services.AddExperimentAuditSink<NoopAuditSink>();

        using var sp = services.BuildServiceProvider();
        var sinks = sp.GetServices<IAuditSink>().ToList();

        // TryAddEnumerable prevents duplicates of same type
        Assert.Single(sinks);
    }

    [Fact]
    public void AddExperimentAuditSink_MultipleDifferentTypes_RegistersAll()
    {
        var services = new ServiceCollection();
        services.AddExperimentAuditSink<NoopAuditSink>();
        services.AddExperimentAuditSink<AnotherNoopAuditSink>();

        using var sp = services.BuildServiceProvider();
        var sinks = sp.GetServices<IAuditSink>().ToList();

        Assert.Equal(2, sinks.Count);
    }

    [Fact]
    public void AddExperimentAuditComposite_WithSingleSink_ReturnsThatSink()
    {
        var services = new ServiceCollection();
        services.AddExperimentAuditSink<NoopAuditSink>();
        services.AddExperimentAuditComposite();

        using var sp = services.BuildServiceProvider();
        var sink = sp.GetRequiredService<IAuditSink>();

        // With a single sink, the composite returns it directly
        Assert.IsType<NoopAuditSink>(sink);
    }

    [Fact]
    public void AddExperimentAuditSink_MultipleDifferentTypes_CanBeEnumeratedTogether()
    {
        // When multiple sinks are registered via AddExperimentAuditSink,
        // they can all be retrieved via GetServices<IAuditSink>().
        // Note: AddExperimentAuditComposite uses TryAddSingleton, which is a no-op
        // when IAuditSink registrations already exist (added via TryAddEnumerable).
        var services = new ServiceCollection();
        services.AddExperimentAuditSink<NoopAuditSink>();
        services.AddExperimentAuditSink<AnotherNoopAuditSink>();
        services.AddExperimentAuditComposite(); // no-op: TryAddSingleton skipped

        using var sp = services.BuildServiceProvider();
        var allSinks = sp.GetServices<IAuditSink>().ToList();

        // Both sinks are enumerable
        Assert.Equal(2, allSinks.Count);
        Assert.Contains(allSinks, s => s is NoopAuditSink);
        Assert.Contains(allSinks, s => s is AnotherNoopAuditSink);
    }

    [Fact]
    public void AddExperimentAuditComposite_WithNoSinks_ReturnsComposite()
    {
        var services = new ServiceCollection();
        services.AddExperimentAuditComposite();

        using var sp = services.BuildServiceProvider();
        var sink = sp.GetRequiredService<IAuditSink>();

        // No sinks registered → composite has 0 sinks
        Assert.IsType<CompositeAuditSink>(sink);
    }

    [Fact]
    public void AddExperimentAuditLogging_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var result = services.AddExperimentAuditLogging();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddExperimentAuditSink_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();
        var result = services.AddExperimentAuditSink<NoopAuditSink>();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddExperimentAuditComposite_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();
        var result = services.AddExperimentAuditComposite();

        Assert.Same(services, result);
    }

    private sealed class NoopAuditSink : IAuditSink
    {
        public ValueTask RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }

    private sealed class AnotherNoopAuditSink : IAuditSink
    {
        public ValueTask RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }
}
