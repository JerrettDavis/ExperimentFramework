using ExperimentFramework.Configuration.Extensions.Handlers;
using ExperimentFramework.Configuration.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExperimentFramework.Configuration.Tests;

/// <summary>
/// Unit tests for the backplane configuration handlers.
/// Tests the Validate() methods which have the most coverage opportunity.
/// </summary>
public sealed class BackplaneHandlerTests
{
    // ===== InMemoryBackplaneConfigurationHandler =====

    [Fact]
    public void InMemoryHandler_BackplaneType_IsInMemory()
    {
        var handler = new InMemoryBackplaneConfigurationHandler();
        Assert.Equal("inMemory", handler.BackplaneType);
    }

    [Fact]
    public void InMemoryHandler_Validate_AlwaysReturnsEmpty()
    {
        var handler = new InMemoryBackplaneConfigurationHandler();
        var config = new DataPlaneBackplaneConfig { Type = "inMemory" };

        var errors = handler.Validate(config, "dataPlane.backplane").ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void InMemoryHandler_Validate_WithOptions_ReturnsEmpty()
    {
        var handler = new InMemoryBackplaneConfigurationHandler();
        var config = new DataPlaneBackplaneConfig
        {
            Type = "inMemory",
            Options = new Dictionary<string, object> { ["key"] = "value" }
        };

        var errors = handler.Validate(config, "backplane").ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void InMemoryHandler_ConfigureServices_ThrowsWhenDataPlaneNotReferenced()
    {
        var handler = new InMemoryBackplaneConfigurationHandler();
        var services = new ServiceCollection();
        var config = new DataPlaneBackplaneConfig { Type = "inMemory" };

        // ExperimentFramework.DataPlane is NOT referenced in this test project,
        // so ConfigureServices should throw an InvalidOperationException.
        Assert.Throws<InvalidOperationException>(() =>
            handler.ConfigureServices(services, config, NullLogger<object>.Instance));
    }

    // ===== LoggingBackplaneConfigurationHandler =====

    [Fact]
    public void LoggingHandler_BackplaneType_IsLogging()
    {
        var handler = new LoggingBackplaneConfigurationHandler();
        Assert.Equal("logging", handler.BackplaneType);
    }

    [Fact]
    public void LoggingHandler_Validate_AlwaysReturnsEmpty()
    {
        var handler = new LoggingBackplaneConfigurationHandler();
        var config = new DataPlaneBackplaneConfig { Type = "logging" };

        var errors = handler.Validate(config, "dataPlane.backplane").ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void LoggingHandler_ConfigureServices_ThrowsWhenDataPlaneNotReferenced()
    {
        var handler = new LoggingBackplaneConfigurationHandler();
        var services = new ServiceCollection();
        var config = new DataPlaneBackplaneConfig { Type = "logging" };

        Assert.Throws<InvalidOperationException>(() =>
            handler.ConfigureServices(services, config, NullLogger<object>.Instance));
    }

    // ===== OpenTelemetryBackplaneConfigurationHandler =====

    [Fact]
    public void OpenTelemetryHandler_BackplaneType_IsOpenTelemetry()
    {
        var handler = new OpenTelemetryBackplaneConfigurationHandler();
        Assert.Equal("openTelemetry", handler.BackplaneType);
    }

    [Fact]
    public void OpenTelemetryHandler_Validate_AlwaysReturnsEmpty()
    {
        var handler = new OpenTelemetryBackplaneConfigurationHandler();
        var config = new DataPlaneBackplaneConfig { Type = "openTelemetry" };

        var errors = handler.Validate(config, "dataPlane.backplane").ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void OpenTelemetryHandler_ConfigureServices_ThrowsWhenDataPlaneNotReferenced()
    {
        var handler = new OpenTelemetryBackplaneConfigurationHandler();
        var services = new ServiceCollection();
        var config = new DataPlaneBackplaneConfig { Type = "openTelemetry" };

        Assert.Throws<InvalidOperationException>(() =>
            handler.ConfigureServices(services, config, NullLogger<object>.Instance));
    }
}
