namespace ExperimentFramework.Science.Models.Snapshots;

/// <summary>
/// Captures environment information for reproducibility.
/// </summary>
/// <remarks>
/// Recording environment details helps ensure experiments can be reproduced
/// and helps diagnose unexpected results due to environmental factors.
/// </remarks>
public sealed class EnvironmentInfo
{
    /// <summary>
    /// Gets the machine name.
    /// </summary>
    public string? MachineName { get; init; }

    /// <summary>
    /// Gets the operating system description.
    /// </summary>
    public string? OperatingSystem { get; init; }

    /// <summary>
    /// Gets the .NET runtime version.
    /// </summary>
    public string? RuntimeVersion { get; init; }

    /// <summary>
    /// Gets the framework version.
    /// </summary>
    public string? FrameworkVersion { get; init; }

    /// <summary>
    /// Gets the processor count.
    /// </summary>
    public int? ProcessorCount { get; init; }

    /// <summary>
    /// Gets the application version.
    /// </summary>
    public string? ApplicationVersion { get; init; }

    /// <summary>
    /// Gets the time zone.
    /// </summary>
    public string? TimeZone { get; init; }

    /// <summary>
    /// Gets custom environment variables.
    /// </summary>
    public IReadOnlyDictionary<string, string>? CustomVariables { get; init; }

    /// <summary>
    /// Captures the current environment.
    /// </summary>
    /// <returns>Environment info for the current system.</returns>
    public static EnvironmentInfo Capture()
    {
        return new EnvironmentInfo
        {
            MachineName = Environment.MachineName,
            OperatingSystem = Environment.OSVersion.ToString(),
            RuntimeVersion = Environment.Version.ToString(),
            FrameworkVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            ProcessorCount = Environment.ProcessorCount,
            TimeZone = TimeZoneInfo.Local.StandardName
        };
    }
}
