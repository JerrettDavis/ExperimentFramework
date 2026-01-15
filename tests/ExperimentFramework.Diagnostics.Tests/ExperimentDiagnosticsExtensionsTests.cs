using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Diagnostics.Tests;

public class ExperimentDiagnosticsExtensionsTests
{
    [Fact]
    public void AddExperimentEventSink_RegistersSinkInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var sink = new InMemoryExperimentEventSink();

        // Act
        services.AddExperimentEventSink(sink);

        // Assert
        var provider = services.BuildServiceProvider();
        var registered = provider.GetService<IExperimentEventSink>();
        Assert.Same(sink, registered);
    }

    [Fact]
    public void AddExperimentEventSink_WithFactory_RegistersSink()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddExperimentEventSink<InMemoryExperimentEventSink>(sp => new InMemoryExperimentEventSink(100));

        // Assert
        var provider = services.BuildServiceProvider();
        var sink = provider.GetService<IExperimentEventSink>() as InMemoryExperimentEventSink;
        Assert.NotNull(sink);
        Assert.Equal(100, sink.MaxCapacity);
    }

    [Fact]
    public void AddExperimentEventSink_WithType_RegistersSink()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddExperimentEventSink<InMemoryExperimentEventSink>();

        // Assert
        var provider = services.BuildServiceProvider();
        var sink = provider.GetService<IExperimentEventSink>();
        Assert.IsType<InMemoryExperimentEventSink>(sink);
    }

    [Fact]
    public void AddInMemoryExperimentEventSink_WithoutCapacity_RegistersUnboundedSink()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInMemoryExperimentEventSink();

        // Assert
        var provider = services.BuildServiceProvider();
        var sink = provider.GetService<InMemoryExperimentEventSink>();
        Assert.NotNull(sink);
        Assert.Null(sink.MaxCapacity);
    }

    [Fact]
    public void AddInMemoryExperimentEventSink_WithCapacity_RegistersBoundedSink()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInMemoryExperimentEventSink(maxCapacity: 50);

        // Assert
        var provider = services.BuildServiceProvider();
        var sink = provider.GetService<InMemoryExperimentEventSink>();
        Assert.NotNull(sink);
        Assert.Equal(50, sink.MaxCapacity);
    }

    [Fact]
    public void AddInMemoryExperimentEventSink_RegistersConcretTypeForDirectAccess()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInMemoryExperimentEventSink();

        // Assert
        var provider = services.BuildServiceProvider();
        var concrete = provider.GetService<InMemoryExperimentEventSink>();
        var interface_ = provider.GetService<IExperimentEventSink>();
        Assert.Same(concrete, interface_);
    }

    [Fact]
    public void AddLoggerExperimentEventSink_RegistersSink()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddLoggerExperimentEventSink();

        // Assert
        var provider = services.BuildServiceProvider();
        var sink = provider.GetService<IExperimentEventSink>();
        Assert.IsType<LoggerExperimentEventSink>(sink);
    }

    [Fact]
    public void AddLoggerExperimentEventSink_WithCategoryName_UsesCategoryName()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddLoggerExperimentEventSink("CustomCategory");

        // Assert
        var provider = services.BuildServiceProvider();
        var sink = provider.GetService<IExperimentEventSink>();
        Assert.NotNull(sink);
    }

    [Fact]
    public void AddOpenTelemetryExperimentEventSink_RegistersSink()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddOpenTelemetryExperimentEventSink();

        // Assert
        var provider = services.BuildServiceProvider();
        var sink = provider.GetService<IExperimentEventSink>();
        Assert.IsType<OpenTelemetryExperimentEventSink>(sink);
    }

    [Fact]
    public void GetExperimentEventSinks_ReturnsNull_WhenNoSinksRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetExperimentEventSinks();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetExperimentEventSinks_ReturnsSingleSink_WhenOneSinkRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        var sink = new InMemoryExperimentEventSink();
        services.AddExperimentEventSink(sink);
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetExperimentEventSinks();

        // Assert
        Assert.Same(sink, result);
    }

    [Fact]
    public void GetExperimentEventSinks_ReturnsCompositeSink_WhenMultipleSinksRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddExperimentEventSink(new InMemoryExperimentEventSink());
        services.AddExperimentEventSink(new InMemoryExperimentEventSink());
        var provider = services.BuildServiceProvider();

        // Act
        var result = provider.GetExperimentEventSinks();

        // Assert
        Assert.IsType<CompositeExperimentEventSink>(result);
        var composite = (CompositeExperimentEventSink)result;
        Assert.Equal(2, composite.SinkCount);
    }

    [Fact]
    public void MultipleSinks_CanBeRegisteredAndRetrieved()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInMemoryExperimentEventSink(100);
        services.AddLoggerExperimentEventSink();
        services.AddOpenTelemetryExperimentEventSink();

        // Act
        var provider = services.BuildServiceProvider();
        var sinks = provider.GetServices<IExperimentEventSink>().ToArray();

        // Assert
        Assert.Equal(3, sinks.Length);
        Assert.Contains(sinks, s => s is InMemoryExperimentEventSink);
        Assert.Contains(sinks, s => s is LoggerExperimentEventSink);
        Assert.Contains(sinks, s => s is OpenTelemetryExperimentEventSink);
    }
}
