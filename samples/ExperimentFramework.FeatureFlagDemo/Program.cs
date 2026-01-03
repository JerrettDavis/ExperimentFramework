using ExperimentFramework;
using ExperimentFramework.FeatureFlagDemo.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;

Console.WriteLine("""
    ╔══════════════════════════════════════════════════════════════════════════════╗
    ║                                                                              ║
    ║               ExperimentFramework - Feature Flag Demo                        ║
    ║                                                                              ║
    ║  Demonstrates integration with Microsoft.FeatureManagement:                  ║
    ║    • UsingFeatureFlag() - Boolean feature flags for A/B testing             ║
    ║    • UsingVariantFeatureFlag() - Multi-variant feature flags (A/B/C/D)      ║
    ║    • Percentage-based rollouts with consistent user assignment              ║
    ║                                                                              ║
    ╚══════════════════════════════════════════════════════════════════════════════╝
    """);

var builder = Host.CreateApplicationBuilder(args);

// Add Feature Management
builder.Services.AddFeatureManagement();

// Register service implementations
builder.Services.AddScoped<ClassicDashboard>();
builder.Services.AddScoped<ModernDashboard>();
builder.Services.AddScoped<ExperimentalDashboard>();

// Register default interface
builder.Services.AddScoped<IDashboard, ClassicDashboard>();

// Configure experiments
var experiments = ConfigureExperiments();
builder.Services.AddExperimentFramework(experiments);

var app = builder.Build();

Console.WriteLine("\n🎯 Simulating dashboard rendering for different users...\n");

// Simulate multiple users accessing the dashboard
var userIds = new[] { "user-101", "user-202", "user-303", "user-404", "user-505" };

using var scope = app.Services.CreateScope();
var dashboard = scope.ServiceProvider.GetRequiredService<IDashboard>();

foreach (var userId in userIds)
{
    Console.WriteLine($"User {userId}:");
    try
    {
        var result = await dashboard.RenderAsync(userId);
        Console.WriteLine($"   {result}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"   ❌ Error: {ex.Message}");
    }
    Console.WriteLine();
}

Console.WriteLine("\n" + new string('═', 80));
Console.WriteLine("DEMO COMPLETE");
Console.WriteLine(new string('═', 80));
Console.WriteLine("""

    Key Takeaways:

    • UsingFeatureFlag() enables simple on/off experiments
    • UsingVariantFeatureFlag() enables multi-variant A/B/C/D testing
    • Combine with Rollout package for percentage-based gradual rollouts
    • Feature flags integrate with Microsoft.FeatureManagement for enterprise features

    """);

static ExperimentFrameworkBuilder ConfigureExperiments()
{
    return ExperimentFrameworkBuilder.Create()
        .UseDispatchProxy() // Use runtime proxies for simplicity
        .Define<IDashboard>(c => c
            // Use feature flag to switch between dashboards
            .UsingFeatureFlag("EnableModernDashboard")
            .AddDefaultTrial<ClassicDashboard>("false")
            .AddTrial<ModernDashboard>("true")
            .OnErrorRedirectAndReplayDefault());
}

namespace ExperimentFramework.FeatureFlagDemo.Services
{
    public interface IDashboard
    {
        Task<string> RenderAsync(string userId);
    }

    public class ClassicDashboard : IDashboard
    {
        public Task<string> RenderAsync(string userId)
        {
            return Task.FromResult($"📊 [CLASSIC] Rendered dashboard for {userId} - Traditional layout");
        }
    }

    public class ModernDashboard : IDashboard
    {
        public Task<string> RenderAsync(string userId)
        {
            return Task.FromResult($"✨ [MODERN] Rendered dashboard for {userId} - New responsive design");
        }
    }

    public class ExperimentalDashboard : IDashboard
    {
        public Task<string> RenderAsync(string userId)
        {
            return Task.FromResult($"🚀 [EXPERIMENTAL] Rendered dashboard for {userId} - Cutting-edge UI");
        }
    }
}
