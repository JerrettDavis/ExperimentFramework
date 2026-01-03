namespace ExperimentFramework.Configuration.Models;

/// <summary>
/// Configuration for data backplane in the DSL.
/// </summary>
public sealed class DataPlaneBackplaneConfig
{
    /// <summary>
    /// Gets or sets the backplane type identifier.
    /// </summary>
    /// <remarks>
    /// Supported types: "inMemory", "logging", "openTelemetry", "kafka", "azureServiceBus", "sqlServer"
    /// Extensions can register additional types.
    /// </remarks>
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets backplane-specific configuration options.
    /// </summary>
    public Dictionary<string, object>? Options { get; set; }
}
