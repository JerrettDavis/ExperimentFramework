using ExperimentFramework;
using ExperimentFramework.DataPlane;
using ExperimentFramework.DataPlane.Abstractions;
using ExperimentFramework.DataPlane.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Build and run application
var builder = Host.CreateApplicationBuilder(args);

// Register concrete implementations
builder.Services.AddSingleton<StripePaymentProcessor>();
builder.Services.AddSingleton<PayPalPaymentProcessor>();
builder.Services.AddSingleton<IPaymentProcessor, StripePaymentProcessor>();

// Register subject identity provider
builder.Services.AddSingleton<ISubjectIdentityProvider, SessionIdentityProvider>();

// Configure data backplane
builder.Services.AddDataBackplane(options =>
{
    options.EnableExposureEvents = true;
    options.SamplingRate = 1.0; // Capture 100% of events
});

// Use in-memory backplane for this sample
builder.Services.AddInMemoryDataBackplane();

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

// Use the service
var processor = app.Services.GetRequiredService<IPaymentProcessor>();
var result = await processor.ProcessPaymentAsync(99.99m);
Console.WriteLine($"Payment processed: {result}");

// Check the data backplane
var backplane = app.Services.GetRequiredService<IDataBackplane>() as InMemoryDataBackplane;
if (backplane != null)
{
    Console.WriteLine($"\n--- Data Backplane Events ---");
    Console.WriteLine($"Total events captured: {backplane.Events.Count}");
    
    foreach (var envelope in backplane.Events)
    {
        Console.WriteLine($"\nEvent ID: {envelope.EventId}");
        Console.WriteLine($"Type: {envelope.EventType}");
        Console.WriteLine($"Timestamp: {envelope.Timestamp:o}");
        Console.WriteLine($"Schema Version: {envelope.SchemaVersion}");
        
        if (envelope.Payload is ExperimentFramework.DataPlane.Abstractions.Events.ExposureEvent exposure)
        {
            Console.WriteLine($"Experiment: {exposure.ExperimentName}");
            Console.WriteLine($"Variant: {exposure.VariantKey}");
            Console.WriteLine($"Subject: {exposure.SubjectId} ({exposure.SubjectType})");
            Console.WriteLine($"Selection Reason: {exposure.SelectionReason}");
        }
    }

    // Check health
    var health = await backplane.HealthAsync();
    Console.WriteLine($"\n--- Backplane Health ---");
    Console.WriteLine($"Healthy: {health.IsHealthy}");
    Console.WriteLine($"Description: {health.Description}");
}

Console.WriteLine("\nSample completed successfully!");

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
