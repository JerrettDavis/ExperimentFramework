namespace ExperimentFramework.DataPlane.Kafka;

/// <summary>
/// Configuration options for the Kafka data backplane.
/// </summary>
public sealed class KafkaDataBackplaneOptions
{
    /// <summary>
    /// Gets or sets the list of Kafka broker addresses.
    /// </summary>
    /// <example>["localhost:9092", "broker2:9092"]</example>
    public required List<string> Brokers { get; set; }

    /// <summary>
    /// Gets or sets the topic name for experiment events.
    /// </summary>
    /// <remarks>
    /// If null, events will be routed to topics based on event type:
    /// - experiment-exposures
    /// - experiment-assignments
    /// - experiment-outcomes
    /// - experiment-analysis-signals
    /// - experiment-errors
    /// </remarks>
    public string? Topic { get; set; }

    /// <summary>
    /// Gets or sets the partitioning strategy.
    /// </summary>
    public KafkaPartitionStrategy PartitionStrategy { get; set; } = KafkaPartitionStrategy.ByExperimentKey;

    /// <summary>
    /// Gets or sets the batch size for producer batching.
    /// </summary>
    public int BatchSize { get; set; } = 500;

    /// <summary>
    /// Gets or sets the linger time in milliseconds for batching.
    /// </summary>
    public int LingerMs { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to enable idempotent producer.
    /// </summary>
    public bool EnableIdempotence { get; set; } = true;

    /// <summary>
    /// Gets or sets the compression type.
    /// </summary>
    public string CompressionType { get; set; } = "snappy";

    /// <summary>
    /// Gets or sets the acknowledgement mode.
    /// </summary>
    /// <remarks>
    /// Valid values: "all" (all replicas), "1" (leader only), "0" (no ack)
    /// </remarks>
    public string Acks { get; set; } = "all";

    /// <summary>
    /// Gets or sets the request timeout in milliseconds.
    /// </summary>
    public int RequestTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the maximum number of in-flight requests per connection.
    /// </summary>
    public int MaxInFlight { get; set; } = 5;

    /// <summary>
    /// Gets or sets the client ID for this producer.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets additional Kafka producer configuration.
    /// </summary>
    public Dictionary<string, string>? AdditionalConfig { get; set; }
}

/// <summary>
/// Partitioning strategies for Kafka events.
/// </summary>
public enum KafkaPartitionStrategy
{
    /// <summary>
    /// Partition by experiment key (service type name).
    /// </summary>
    ByExperimentKey,

    /// <summary>
    /// Partition by subject ID.
    /// </summary>
    BySubjectId,

    /// <summary>
    /// Partition by tenant ID.
    /// </summary>
    ByTenantId,

    /// <summary>
    /// Round-robin partitioning.
    /// </summary>
    RoundRobin
}
