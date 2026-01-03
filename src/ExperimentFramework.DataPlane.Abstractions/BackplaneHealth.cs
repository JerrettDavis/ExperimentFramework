namespace ExperimentFramework.DataPlane.Abstractions;

/// <summary>
/// Represents the health status of a data backplane.
/// </summary>
public sealed class BackplaneHealth
{
    /// <summary>
    /// Gets or sets whether the backplane is healthy.
    /// </summary>
    public required bool IsHealthy { get; init; }

    /// <summary>
    /// Gets or sets a description of the health status.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the timestamp of the health check.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets additional diagnostic information.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Diagnostics { get; init; }

    /// <summary>
    /// Creates a healthy backplane status.
    /// </summary>
    public static BackplaneHealth Healthy(string? description = null) =>
        new() { IsHealthy = true, Description = description };

    /// <summary>
    /// Creates an unhealthy backplane status.
    /// </summary>
    public static BackplaneHealth Unhealthy(string? description = null, 
        IReadOnlyDictionary<string, object>? diagnostics = null) =>
        new() { IsHealthy = false, Description = description, Diagnostics = diagnostics };
}
