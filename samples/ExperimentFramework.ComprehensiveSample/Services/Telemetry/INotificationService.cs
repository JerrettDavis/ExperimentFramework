namespace ExperimentFramework.ComprehensiveSample.Services.Telemetry;

/// <summary>
/// Service demonstrating OpenTelemetry Activity tracking
/// </summary>
public interface INotificationService
{
    Task SendAsync(string recipient, string message);
}

public class EmailNotificationService : INotificationService
{
    public async Task SendAsync(string recipient, string message)
    {
        Console.WriteLine($"    → EmailNotificationService: Sending email to {recipient}");
        await Task.Delay(50); // Simulate email sending
        Console.WriteLine("    → Email sent successfully");
    }
}

public class SmsNotificationService : INotificationService
{
    public async Task SendAsync(string recipient, string message)
    {
        Console.WriteLine($"    → SmsNotificationService: Sending SMS to {recipient}");
        await Task.Delay(30); // Simulate SMS sending
        Console.WriteLine("    → SMS sent successfully");
    }
}
