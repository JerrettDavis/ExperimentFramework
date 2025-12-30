namespace ExperimentFramework.DataPlane.SqlServer.Data;

/// <summary>
/// Entity representing an experiment event in SQL Server.
/// </summary>
public sealed class ExperimentEventEntity
{
    /// <summary>
    /// Gets or sets the unique database identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the event ID from the data plane envelope.
    /// </summary>
    public required string EventId { get; set; }

    /// <summary>
    /// Gets or sets the event timestamp.
    /// </summary>
    public required DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public required string EventType { get; set; }

    /// <summary>
    /// Gets or sets the schema version.
    /// </summary>
    public required string SchemaVersion { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized payload.
    /// </summary>
    public required string PayloadJson { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized metadata.
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Gets or sets when the record was created in the database.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
