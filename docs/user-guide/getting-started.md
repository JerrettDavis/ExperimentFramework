# Getting Started

This guide walks you through installing ExperimentFramework and creating your first experiment. By the end, you'll have a working application that switches between two database implementations based on a feature flag.

## Prerequisites

- .NET 10.0 SDK or later
- A .NET project (console app, ASP.NET Core, or worker service)
- Basic familiarity with dependency injection in .NET

## Installation

Add the ExperimentFramework package to your project:

```bash
dotnet add package ExperimentFramework
```

**Choose your proxy mode:**

**Option A: Source-Generated Proxies (Recommended)**
```bash
dotnet add package ExperimentFramework.Generators
```

This enables compile-time proxy generation with near-zero overhead (<100ns per call). Requires using `[ExperimentCompositionRoot]` attribute or calling `.UseSourceGenerators()`.

**Option B: Runtime Proxies (Alternative)**

No additional package needed. Use `.UseDispatchProxy()` in your configuration. This has higher overhead (~800ns per call) but offers more flexibility and easier debugging.

**For feature flag support:**

```bash
dotnet add package Microsoft.FeatureManagement
```

## Create a Simple Experiment

### Step 1: Define Your Service Interface

Create an interface that represents the service you want to experiment with:

```csharp
public interface IDatabase
{
    Task<string> GetConnectionStringAsync();
    Task<IEnumerable<Customer>> GetCustomersAsync();
}
```

### Step 2: Implement Multiple Versions

Create two implementations of this interface:

```csharp
public class LocalDatabase : IDatabase
{
    public Task<string> GetConnectionStringAsync()
    {
        return Task.FromResult("Server=localhost;Database=MyApp");
    }

    public async Task<IEnumerable<Customer>> GetCustomersAsync()
    {
        // Simulate database query
        await Task.Delay(50);
        return new List<Customer>
        {
            new("Alice", "alice@example.com"),
            new("Bob", "bob@example.com")
        };
    }
}

public class CloudDatabase : IDatabase
{
    public Task<string> GetConnectionStringAsync()
    {
        return Task.FromResult("Server=cloud.example.com;Database=MyApp");
    }

    public async Task<IEnumerable<Customer>> GetCustomersAsync()
    {
        // Simulate cloud database query
        await Task.Delay(30);
        return new List<Customer>
        {
            new("Alice", "alice@example.com"),
            new("Bob", "bob@example.com"),
            new("Charlie", "charlie@example.com")
        };
    }
}

public record Customer(string Name, string Email);
```

### Step 3: Register Services with Dependency Injection

In your `Program.cs`, register both implementations:

```csharp
using ExperimentFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;

var builder = Host.CreateApplicationBuilder(args);

// Register feature management
builder.Services.AddFeatureManagement();

// Register both database implementations
builder.Services.AddScoped<LocalDatabase>();
builder.Services.AddScoped<CloudDatabase>();

// Register the default implementation
builder.Services.AddScoped<IDatabase, LocalDatabase>();
```

### Step 4: Define the Experiment

Configure the experiment using the fluent builder API.

**Option A: Using Source-Generated Proxies (Fast)**

Add a method with the `[ExperimentCompositionRoot]` attribute:

```csharp
[ExperimentCompositionRoot]
public static ExperimentFrameworkBuilder ConfigureExperiments()
{
    return ExperimentFrameworkBuilder.Create()
        .Trial<IDatabase>(trial => trial
            .UsingFeatureFlag("UseCloudDb")
            .AddControl<LocalDatabase>()
            .AddCondition<CloudDatabase>("true")
            .OnErrorFallbackToControl());
}

// Register the experiment framework
var experiments = ConfigureExperiments();
builder.Services.AddExperimentFramework(experiments);
```

The `[ExperimentCompositionRoot]` attribute triggers source generation at compile time.

**Option B: Using Runtime Proxies (Flexible)**

```csharp
public static ExperimentFrameworkBuilder ConfigureExperiments()
{
    return ExperimentFrameworkBuilder.Create()
        .Trial<IDatabase>(trial => trial
            .UsingFeatureFlag("UseCloudDb")
            .AddControl<LocalDatabase>()
            .AddCondition<CloudDatabase>("true")
            .OnErrorFallbackToControl())
        .UseDispatchProxy(); // Use runtime proxies
}

// Register the experiment framework
var experiments = ConfigureExperiments();
builder.Services.AddExperimentFramework(experiments);
```

**What this code does:**

- `ExperimentFrameworkBuilder.Create()` creates a new builder
- `.Trial<IDatabase>()` defines a trial for the IDatabase interface
- `.UsingFeatureFlag("UseCloudDb")` means the selection is based on a boolean feature flag
- `.AddControl<LocalDatabase>()` sets LocalDatabase as the control (stable baseline)
- `.AddCondition<CloudDatabase>("true")` adds CloudDatabase as a condition (experimental)
- `.OnErrorFallbackToControl()` means if CloudDatabase throws, fall back to LocalDatabase
- `[ExperimentCompositionRoot]` or `.UseDispatchProxy()` determines which proxy mode to use

### Step 5: Configure the Feature Flag

Create or modify `appsettings.json`:

```json
{
  "FeatureManagement": {
    "UseCloudDb": false
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Step 6: Use the Service

Your application code doesn't need to change. Just inject IDatabase as usual:

```csharp
public class CustomerService
{
    private readonly IDatabase _database;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(IDatabase database, ILogger<CustomerService> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task DisplayCustomersAsync()
    {
        var connectionString = await _database.GetConnectionStringAsync();
        _logger.LogInformation("Connected to: {ConnectionString}", connectionString);

        var customers = await _database.GetCustomersAsync();
        foreach (var customer in customers)
        {
            _logger.LogInformation("Customer: {Name} - {Email}", customer.Name, customer.Email);
        }
    }
}
```

### Step 7: Run the Application

Complete `Program.cs`:

```csharp
using ExperimentFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

var builder = Host.CreateApplicationBuilder(args);

// Register services
builder.Services.AddLogging(logging => logging.AddConsole());
builder.Services.AddFeatureManagement();

// Register database implementations
builder.Services.AddScoped<LocalDatabase>();
builder.Services.AddScoped<CloudDatabase>();
builder.Services.AddScoped<IDatabase, LocalDatabase>();

// Define experiment
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IDatabase>(trial => trial
        .UsingFeatureFlag("UseCloudDb")
        .AddControl<LocalDatabase>()
        .AddCondition<CloudDatabase>("true")
        .OnErrorFallbackToControl());

builder.Services.AddExperimentFramework(experiments);

// Register application service
builder.Services.AddScoped<CustomerService>();

var app = builder.Build();

// Execute the service
using (var scope = app.Services.CreateScope())
{
    var customerService = scope.ServiceProvider.GetRequiredService<CustomerService>();
    await customerService.DisplayCustomersAsync();
}
```

Run the application:

```bash
dotnet run
```

You should see output showing the local database connection:

```
info: CustomerService[0]
      Connected to: Server=localhost;Database=MyApp
info: CustomerService[0]
      Customer: Alice - alice@example.com
info: CustomerService[0]
      Customer: Bob - bob@example.com
```

### Step 8: Enable the Experiment

Change the feature flag in `appsettings.json`:

```json
{
  "FeatureManagement": {
    "UseCloudDb": true
  }
}
```

Run again:

```bash
dotnet run
```

Now you should see the cloud database:

```
info: CustomerService[0]
      Connected to: Server=cloud.example.com;Database=MyApp
info: CustomerService[0]
      Customer: Alice - alice@example.com
info: CustomerService[0]
      Customer: Bob - bob@example.com
info: CustomerService[0]
      Customer: Charlie - charlie@example.com
```

## What Just Happened?

When you requested `IDatabase` from the dependency injection container:

1. The framework provided a proxy that selects the appropriate implementation
2. The proxy evaluated the `UseCloudDb` feature flag
3. Based on the flag's value, it resolved either `LocalDatabase` or `CloudDatabase`
4. Method calls were forwarded to the selected implementation
5. Results were returned transparently to your code

Your `CustomerService` class never knew it was talking to a proxy. It just used the `IDatabase` interface normally.

## Adding Observability

To see what's happening under the hood, add logging decorators:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .AddLogger(logging => logging
        .AddBenchmarks()
        .AddErrorLogging())
    .Trial<IDatabase>(trial => trial
        .UsingFeatureFlag("UseCloudDb")
        .AddControl<LocalDatabase>()
        .AddCondition<CloudDatabase>("true")
        .OnErrorFallbackToControl());
```

Now when you run the application, you'll see additional logs showing trial selection and timing:

```
info: ExperimentFramework.Benchmarks[0]
      Experiment call: IDatabase.GetConnectionStringAsync trial=true elapsedMs=1.2
info: ExperimentFramework.Benchmarks[0]
      Experiment call: IDatabase.GetCustomersAsync trial=true elapsedMs=31.4
```

## Next Steps

Now that you have a working experiment, you can explore:

- [Core Concepts](core-concepts.md) - Understand how trials, proxies, and decorators work
- [Selection Modes](selection-modes.md) - Learn about configuration values, variants, and sticky routing
- [Error Handling](error-handling.md) - Explore different error handling strategies
- [Telemetry](telemetry.md) - Integrate with OpenTelemetry for distributed tracing

## Common Issues

### The experiment always uses the default trial

Make sure you've:
- Registered both implementations with the DI container
- Called `AddFeatureManagement()` before `AddExperimentFramework()`
- Set the feature flag value in configuration
- Used the correct feature flag name in both experiment definition and configuration

### Type not registered exception

Ensure both trial types are registered with the DI container before calling `AddExperimentFramework()`:

```csharp
services.AddScoped<LocalDatabase>();  // Must be registered
services.AddScoped<CloudDatabase>();  // Must be registered
```

### Configuration not updating at runtime

If you're using `appsettings.json`, make sure your configuration source is set to reload on change:

```csharp
builder.Configuration.AddJsonFile("appsettings.json",
    optional: false,
    reloadOnChange: true);
```
