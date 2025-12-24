using ExperimentFramework.ComprehensiveSample.Services.Telemetry;
using System.Diagnostics;

namespace ExperimentFramework.ComprehensiveSample.Demos;

/// <summary>
/// Demonstrates OpenTelemetry integration for distributed tracing of experiments
/// </summary>
public class OpenTelemetryDemo(INotificationService notificationService)
{
    public async Task RunAsync()
    {
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("DEMO 3: OPENTELEMETRY INTEGRATION");
        Console.WriteLine(new string('=', 80));

        Console.WriteLine("\nOpenTelemetry integration provides:");
        Console.WriteLine("  - Automatic Activity (span) creation for each experiment invocation");
        Console.WriteLine("  - Experiment metadata as Activity tags (service type, method, trial key)");
        Console.WriteLine("  - Success/failure tracking");
        Console.WriteLine("  - Distributed tracing context propagation");

        Console.WriteLine("\nWatching for Activities with source 'ExperimentFramework':");

        // Set up Activity listener to capture telemetry
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "ExperimentFramework",
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity =>
            {
                Console.WriteLine($"\n  [OpenTelemetry] Activity Started:");
                Console.WriteLine($"    Name: {activity.DisplayName}");
                Console.WriteLine($"    OperationName: {activity.OperationName}");
                foreach (var tag in activity.TagObjects)
                {
                    Console.WriteLine($"    Tag: {tag.Key} = {tag.Value}");
                }
            },
            ActivityStopped = activity =>
            {
                Console.WriteLine($"  [OpenTelemetry] Activity Stopped:");
                Console.WriteLine($"    Duration: {activity.Duration.TotalMilliseconds}ms");
                Console.WriteLine($"    Status: {activity.Status}");
            }
        };

        ActivitySource.AddActivityListener(listener);

        Console.WriteLine("\nSending notification (triggers experiment with telemetry):");
        await notificationService.SendAsync("user@example.com", "Test message");

        Console.WriteLine("\n  → Activity captured experiment execution details");
        Console.WriteLine("  → In production, export to Jaeger, Zipkin, Application Insights, etc.");
    }
}
