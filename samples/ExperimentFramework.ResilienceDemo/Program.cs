using ExperimentFramework;
using ExperimentFramework.ResilienceDemo.Services;
using ExperimentFramework.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.WriteLine("""
    ╔══════════════════════════════════════════════════════════════════════════════╗
    ║                                                                              ║
    ║               ExperimentFramework - Resilience Demo                          ║
    ║                                                                              ║
    ║  Demonstrates error handling and fallback patterns:                          ║
    ║    • OnErrorRedirectAndReplayDefault - Fall back to default implementation   ║
    ║    • OnErrorRedirectAndReplayAny - Try all implementations until one works   ║
    ║    • OnErrorRedirectAndReplayOrdered - Try fallbacks in specific order       ║
    ║                                                                              ║
    ╚══════════════════════════════════════════════════════════════════════════════╝
    """);

var builder = Host.CreateApplicationBuilder(args);

// Register service implementations
builder.Services.AddScoped<PrimaryPaymentGateway>();
builder.Services.AddScoped<BackupPaymentGateway>();
builder.Services.AddScoped<OfflinePaymentProcessor>();

// Register default interface
builder.Services.AddScoped<IPaymentGateway, PrimaryPaymentGateway>();

// Configure experiment with ordered fallback
var experiments = ConfigureExperiments();
builder.Services.AddExperimentFramework(experiments);

var app = builder.Build();

Console.WriteLine("\n🎯 Running payment processing simulation...\n");

using var scope = app.Services.CreateScope();
var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentGateway>();

// Simulate multiple payment attempts
var paymentIds = new[] { "PAY-001", "PAY-002", "PAY-003", "PAY-004", "PAY-005" };

foreach (var paymentId in paymentIds)
{
    try
    {
        Console.WriteLine($"Processing payment {paymentId}...");
        var result = await paymentService.ProcessPaymentAsync(paymentId, 99.99m);
        Console.WriteLine($"   ✅ {result}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"   ❌ Failed: {ex.Message}");
    }
    Console.WriteLine();
}

Console.WriteLine("\n" + new string('═', 80));
Console.WriteLine("DEMO COMPLETE");
Console.WriteLine(new string('═', 80));
Console.WriteLine("""

    Key Takeaways:

    • OnErrorRedirectAndReplayOrdered tries fallbacks in your specified order
    • The framework automatically routes failed calls to fallback implementations
    • This provides graceful degradation without code changes in your services
    • Combine with circuit breakers for even more resilient systems

    """);

static ExperimentFrameworkBuilder ConfigureExperiments()
{
    return ExperimentFrameworkBuilder.Create()
        .UseDispatchProxy() // Use runtime proxies for simplicity
        .Define<IPaymentGateway>(c => c
            .UsingConfigurationKey("PaymentGateway:Provider")
            .AddDefaultTrial<PrimaryPaymentGateway>("primary")
            .AddTrial<BackupPaymentGateway>("backup")
            .AddTrial<OfflinePaymentProcessor>("offline")
            // If primary fails, try backup, then offline
            .OnErrorRedirectAndReplayOrdered("backup", "offline"));
}

namespace ExperimentFramework.ResilienceDemo.Services
{
    public interface IPaymentGateway
    {
        Task<string> ProcessPaymentAsync(string paymentId, decimal amount);
    }

    public class PrimaryPaymentGateway : IPaymentGateway
    {
        private static int _callCount;

        public async Task<string> ProcessPaymentAsync(string paymentId, decimal amount)
        {
            await Task.Delay(50); // Simulate network latency

            _callCount++;
            // Fail on 2nd and 4th calls to demonstrate fallback
            if (_callCount is 2 or 4)
            {
                throw new HttpRequestException("Primary gateway unavailable");
            }

            return $"PRIMARY processed {paymentId} for ${amount:N2}";
        }
    }

    public class BackupPaymentGateway : IPaymentGateway
    {
        private static int _callCount;

        public async Task<string> ProcessPaymentAsync(string paymentId, decimal amount)
        {
            await Task.Delay(100); // Backup is slower

            _callCount++;
            // Fail on first backup call to show cascading fallback
            if (_callCount == 1)
            {
                throw new HttpRequestException("Backup gateway overloaded");
            }

            return $"BACKUP processed {paymentId} for ${amount:N2}";
        }
    }

    public class OfflinePaymentProcessor : IPaymentGateway
    {
        public Task<string> ProcessPaymentAsync(string paymentId, decimal amount)
        {
            // Offline processor never fails (stores for later processing)
            return Task.FromResult($"OFFLINE queued {paymentId} for ${amount:N2} (will process later)");
        }
    }
}
