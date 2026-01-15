# ExperimentFramework.Testing

Testing utilities for ExperimentFramework that enable deterministic testing of experiments without heavy mocking or complex setup.

## Features

- **Deterministic Routing**: Force control/condition selection per trial/service
- **Test Harness**: First-class DI + experiment test host
- **Trace Capture**: In-memory event sink for assertions
- **Framework-Agnostic**: Works with any test framework (xUnit, NUnit, MSTest)
- **Proxy Strategy Testing**: Run tests across all proxy strategies

## Installation

```bash
dotnet add package ExperimentFramework.Testing
```

## Quick Start

### Basic Test with Forced Selection

```csharp
using ExperimentFramework.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class MyDatabaseTests
{
    [Fact]
    public async Task Should_UseCloudDatabase_WhenForced()
    {
        // Arrange
        var host = ExperimentTestHost.Create(services =>
        {
            services.AddScoped<IMyDatabase, MyDatabase>();
            services.AddScoped<MyDatabase>();
            services.AddScoped<CloudDatabase>();
        })
        .WithExperiments(experiments => experiments
            .Trial<IMyDatabase>(trial => trial
                .UsingTest()
                .AddControl<MyDatabase>()
                .AddCondition<CloudDatabase>("cloud"))
            .UseDispatchProxy())
        .Build();

        // Act
        using var scope = ExperimentTestScope.Begin()
            .ForceCondition<IMyDatabase>("cloud");

        var db = host.Services.GetRequiredService<IMyDatabase>();
        var result = await db.GetConnectionStringAsync();

        // Assert
        Assert.Equal("cloud.example.com", result);
        Assert.True(host.Trace.ExpectRouted<IMyDatabase>("cloud"));
    }
}
```

### Frozen Selection for Deterministic Behavior

```csharp
[Fact]
public void Should_MaintainSameSelection_WhenFrozen()
{
    var host = ExperimentTestHost.Create(services =>
    {
        services.AddScoped<IMyDatabase, MyDatabase>();
        services.AddScoped<MyDatabase>();
        services.AddScoped<CloudDatabase>();
    })
    .WithExperiments(experiments => experiments
        .Trial<IMyDatabase>(trial => trial
            .UsingTest()
            .AddControl<MyDatabase>()
            .AddCondition<CloudDatabase>("cloud"))
        .UseDispatchProxy())
    .Build();

    using var scope = ExperimentTestScope.Begin()
        .ForceCondition<IMyDatabase>("cloud")
        .FreezeSelection();

    // Multiple calls will use the same selection
    var db1 = host.Services.GetRequiredService<IMyDatabase>();
    var db2 = host.Services.GetRequiredService<IMyDatabase>();
    
    var result1 = await db1.QueryAsync();
    var result2 = await db2.QueryAsync();
    
    // Both should use CloudDatabase
    Assert.Equal("cloud-result", result1);
    Assert.Equal("cloud-result", result2);
}
```

### Running Tests Across All Proxy Strategies

```csharp
[Fact]
public void Should_WorkWithAllProxyStrategies()
{
    ExperimentTestMatrix.RunInAllProxyModes(
        configure: builder => builder
            .Trial<IMyService>(t => t
                .UsingTest()
                .AddControl<DefaultImpl>()
                .AddCondition<TestImpl>("test")),
        test: sp =>
        {
            using var scope = ExperimentTestScope.Begin()
                .ForceCondition<IMyService>("test");
            
            var svc = sp.GetRequiredService<IMyService>();
            Assert.Equal(42, svc.GetValue());
        });
}
```

### Trace Assertions

```csharp
[Fact]
public async Task Should_CaptureTraceEvents()
{
    var host = ExperimentTestHost.Create(services =>
    {
        services.AddScoped<IMyDatabase, MyDatabase>();
        services.AddScoped<MyDatabase>();
    })
    .WithExperiments(experiments => experiments
        .Trial<IMyDatabase>(trial => trial
            .UsingTest()
            .AddControl<MyDatabase>()))
    .Build();

    var db = host.Services.GetRequiredService<IMyDatabase>();
    await db.QueryAsync();

    // Assert using trace
    Assert.True(host.Trace.ExpectRouted<IMyDatabase>("control"));
    Assert.True(host.Trace.ExpectCall<IMyDatabase>("QueryAsync"));
    
    var events = host.Trace.GetEventsFor<IMyDatabase>();
    Assert.Single(events);
}
```

## API Reference

### ExperimentTestHost

Fluent builder for creating test hosts with experiments.

```csharp
var host = ExperimentTestHost.Create(services => { ... })
    .WithExperiments(experiments => { ... })
    .Build();

// Access service provider
var service = host.Services.GetRequiredService<IMyService>();

// Access trace for assertions
Assert.True(host.Trace.ExpectRouted<IMyService>("trial-key"));

// Access raw event sink
var events = host.EventSink.Events;
```

### ExperimentTestScope

Scoped context for forcing trial selection.

```csharp
using var scope = ExperimentTestScope.Begin()
    .ForceControl<IMyService>()
    .ForceCondition<IOtherService>("variant-a")
    .FreezeSelection();
```

Methods:
- `ForceControl<TService>()` - Force control implementation
- `ForceCondition<TService>(string key)` - Force specific condition
- `ForceTrialKey<TService>(string key)` - Force any trial key
- `FreezeSelection()` - Lock selection for scope duration

### ExperimentTraceAssertions

Framework-agnostic assertion helpers.

```csharp
host.Trace.ExpectRouted<IMyService>("trial-key");
host.Trace.ExpectFallback<IMyService>();
host.Trace.ExpectCall<IMyService>("MethodName");
host.Trace.ExpectException<IMyService>();

var events = host.Trace.GetEventsFor<IMyService>();
var firstEvent = host.Trace.GetFirstEventFor<IMyService>();
```

### ExperimentTestMatrix

Run tests across multiple proxy strategies.

```csharp
ExperimentTestMatrix.RunInAllProxyModes(
    configure: builder => { ... },
    test: sp => { ... },
    options: new ExperimentTestMatrixOptions
    {
        Strategies = new[] { ProxyStrategy.SourceGenerated, ProxyStrategy.DispatchProxy },
        StopOnFirstFailure = false
    });
```

### ServiceExperimentBuilderExtensions

Extension method for test selection mode.

```csharp
.Trial<IMyService>(trial => trial
    .UsingTest()  // Use deterministic test selection
    .AddControl<DefaultImpl>()
    .AddCondition<TestImpl>("test"))
```

## Design Notes

- **No Hard Dependencies**: Works with any test framework
- **Ambient Context**: Uses `AsyncLocal<T>` for async-safe scoping
- **Thread-Safe**: All components designed for concurrent use
- **Minimal Overhead**: Lightweight event capture with no production impact

## Examples

See the `tests/ExperimentFramework.Testing.Tests` directory for comprehensive examples.

## Contributing

Contributions welcome! Please ensure tests pass and follow existing patterns.

## License

MIT
