using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.ServiceRegistration;

/// <summary>
/// Immutable snapshot of original service descriptors (pre-mutation).
/// </summary>
/// <remarks>
/// <para>
/// This snapshot is captured before any experiment framework mutations are applied
/// to the IServiceCollection. It provides a stable reference point for:
/// </para>
/// <list type="bullet">
/// <item><description>Validation of proposed changes</description></item>
/// <item><description>Audit and traceability</description></item>
/// <item><description>Rollback scenarios</description></item>
/// <item><description>Support and debugging</description></item>
/// </list>
/// </remarks>
public sealed class ServiceGraphSnapshot
{
    /// <summary>
    /// Gets the unique identifier for this snapshot.
    /// </summary>
    public string SnapshotId { get; }

    /// <summary>
    /// Gets the timestamp when this snapshot was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the immutable list of service descriptors at the time of snapshot.
    /// </summary>
    public IReadOnlyList<ServiceDescriptor> Descriptors { get; }

    /// <summary>
    /// Gets the computed fingerprint of the service graph for change detection.
    /// </summary>
    public string Fingerprint { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceGraphSnapshot"/> class.
    /// </summary>
    /// <param name="snapshotId">The unique identifier for this snapshot.</param>
    /// <param name="timestamp">The timestamp when the snapshot was created.</param>
    /// <param name="descriptors">The service descriptors to capture.</param>
    /// <param name="fingerprint">The computed fingerprint of the service graph.</param>
    public ServiceGraphSnapshot(
        string snapshotId,
        DateTimeOffset timestamp,
        IReadOnlyList<ServiceDescriptor> descriptors,
        string fingerprint)
    {
        SnapshotId = snapshotId ?? throw new ArgumentNullException(nameof(snapshotId));
        Timestamp = timestamp;
        Descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
        Fingerprint = fingerprint ?? throw new ArgumentNullException(nameof(fingerprint));
    }

    /// <summary>
    /// Creates a snapshot from the current service collection.
    /// </summary>
    /// <param name="services">The service collection to snapshot.</param>
    /// <returns>A new snapshot instance.</returns>
    public static ServiceGraphSnapshot Capture(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var snapshotId = Guid.NewGuid().ToString("N");
        var timestamp = DateTimeOffset.UtcNow;
        var descriptors = services.ToArray();
        var fingerprint = ComputeFingerprint(descriptors);

        return new ServiceGraphSnapshot(snapshotId, timestamp, descriptors, fingerprint);
    }

    /// <summary>
    /// Computes a fingerprint for the descriptor collection for change detection.
    /// </summary>
    /// <param name="descriptors">The descriptors to fingerprint.</param>
    /// <returns>A fingerprint string.</returns>
    private static string ComputeFingerprint(ServiceDescriptor[] descriptors)
    {
        // Compute a hash-based fingerprint using all service types
        var typeNames = descriptors
            .Select(d => d.ServiceType.FullName)
            .Order()
            .ToArray();

        // Use a simple but complete hash of all service types
        var combined = string.Join("|", typeNames);
        var hash = combined.GetHashCode();

        return $"{descriptors.Length}:{hash:X8}";
    }
}
