using ExperimentFramework;
using ExperimentFramework.DataPlane;
using ExperimentFramework.DataPlane.Abstractions;
using ExperimentFramework.DataPlane.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

// Custom Activity listener to capture OpenTelemetry activities
var activityListener = new ActivityListener
{
    ShouldListenTo = source => source.Name == "ExperimentFramework.DataPlane",
    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
    ActivityStarted = activity =>
    {
        Console.WriteLine($"\n[Activity Started] {activity.DisplayName}");
        Console.WriteLine($"  Activity ID: {activity.Id}");
        foreach (var tag in activity.Tags)
        {
            Console.WriteLine($"  {tag.Key}: {tag.Value}");
        }
    }
};

ActivitySource.AddActivityListener(activityListener);

// Build and run application
var builder = Host.CreateApplicationBuilder(args);

// Register concrete implementations
builder.Services.AddSingleton<StripePaymentProcessor>();
builder.Services.AddSingleton<PayPalPaymentProcessor>();
builder.Services.AddSingleton<IPaymentProcessor, StripePaymentProcessor>();

// Register subject identity provider
builder.Services.AddSingleton<ISubjectIdentityProvider, SessionIdentityProvider>();

// Configure data backplane with OpenTelemetry
builder.Services.AddDataBackplane(options =>
{
    options.EnableExposureEvents = true;
    options.SamplingRate = 1.0; // Capture 100% of events
});

// Use OpenTelemetry backplane
builder.Services.AddOpenTelemetryDataBackplane();

// Configure experiment with exposure logging
var experiments = ExperimentFrameworkBuilder.Create()
    .AddLogger(l => l.AddBenchmarks().AddErrorLogging())
    .WithExposureLogging()  // Enable exposure event emission
    .Trial<IPaymentProcessor>(t => t
        .UsingConfigurationKey("PaymentProvider")
        .AddControl<StripePaymentProcessor>()
        .AddVariant<PayPalPaymentProcessor>("paypal")
        .OnErrorFallbackToControl())
    .UseDispatchProxy();

builder.Services.AddExperimentFramework(experiments);

var app = builder.Build();

Console.WriteLine("=== OpenTelemetry Data Backplane Sample ===\n");
Console.WriteLine("This sample demonstrates OpenTelemetry integration with the data backplane.");
Console.WriteLine("Activities are emitted as spans with semantic tags for distributed tracing.\n");

// Use the service - this will emit an exposure event as an OpenTelemetry activity
var processor = app.Services.GetRequiredService<IPaymentProcessor>();
var result = await processor.ProcessPaymentAsync(99.99m);
Console.WriteLine($"\nPayment processed: {result}");

// Check the data backplane health
var backplane = app.Services.GetRequiredService<IDataBackplane>();
var health = await backplane.HealthAsync();
Console.WriteLine($"\n--- Backplane Health ---");
Console.WriteLine($"Healthy: {health.IsHealthy}");
Console.WriteLine($"Description: {health.Description}");

Console.WriteLine("\n=== Sample Completed ===");
Console.WriteLine("\nNote: In production, configure OpenTelemetry SDK with exporters:");
Console.WriteLine("  builder.Services.AddOpenTelemetry()");
Console.WriteLine("      .WithTracing(tracing => tracing");
Console.WriteLine("          .AddSource(\"ExperimentFramework.DataPlane\")");
Console.WriteLine("          .AddConsoleExporter());  // or other exporters");

activityListener.Dispose();

// Sample service interface
public interface IPaymentProcessor
{
    Task<string> ProcessPaymentAsync(decimal amount);
}

// Control implementation
public class StripePaymentProcessor : IPaymentProcessor
{
    private readonly ILogger<StripePaymentProcessor> _logger;

    public StripePaymentProcessor(ILogger<StripePaymentProcessor> logger)
    {
        _logger = logger;
    }

    public Task<string> ProcessPaymentAsync(decimal amount)
    {
        _logger.LogInformation("Processing ${Amount} via Stripe", amount);
        return Task.FromResult($"Stripe-{Guid.NewGuid():N}");
    }
}

// Variant implementation
public class PayPalPaymentProcessor : IPaymentProcessor
{
    private readonly ILogger<PayPalPaymentProcessor> _logger;

    public PayPalPaymentProcessor(ILogger<PayPalPaymentProcessor> logger)
    {
        _logger = logger;
    }

    public Task<string> ProcessPaymentAsync(decimal amount)
    {
        _logger.LogInformation("Processing ${Amount} via PayPal", amount);
        return Task.FromResult($"PayPal-{Guid.NewGuid():N}");
    }
}

// Subject identity provider for exposure logging
public class SessionIdentityProvider : ISubjectIdentityProvider
{
    private readonly string _sessionId = Guid.NewGuid().ToString("N");

    public string SubjectType => "session";

    public bool TryGetSubjectId(out string subjectId)
    {
        subjectId = _sessionId;
        return true;
    }
}
