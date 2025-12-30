namespace ExperimentFramework.DataPlane.AzureServiceBus;

/// <summary>
/// Configuration options for the Azure Service Bus data backplane.
/// </summary>
public sealed class AzureServiceBusDataBackplaneOptions
{
    /// <summary>
    /// Gets or sets the connection string for Azure Service Bus.
    /// </summary>
    public required string ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the queue name for experiment events.
    /// If set, queue mode is used instead of topic/subscription.
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// Gets or sets the topic name for experiment events.
    /// Used when QueueName is not specified.
    /// </summary>
    public string? TopicName { get; set; }

    /// <summary>
    /// Gets or sets whether to use type-specific queues/topics.
    /// </summary>
    /// <remarks>
    /// If true and no QueueName/TopicName is specified, events are routed to type-specific destinations:
    /// - experiment-exposures
    /// - experiment-assignments
    /// - experiment-outcomes
    /// - experiment-analysis-signals
    /// - experiment-errors
    /// </remarks>
    public bool UseTypeSpecificDestinations { get; set; } = false;

    /// <summary>
    /// Gets or sets the message time-to-live in minutes.
    /// </summary>
    public int? MessageTimeToLiveMinutes { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the batch size for sending messages.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to enable session support for ordering guarantees.
    /// </summary>
    public bool EnableSessions { get; set; } = false;

    /// <summary>
    /// Gets or sets the session grouping strategy when sessions are enabled.
    /// </summary>
    public ServiceBusSessionStrategy SessionStrategy { get; set; } = ServiceBusSessionStrategy.ByExperimentKey;

    /// <summary>
    /// Gets or sets the client identifier.
    /// </summary>
    public string? ClientId { get; set; }
}

/// <summary>
/// Session grouping strategies for Azure Service Bus.
/// </summary>
public enum ServiceBusSessionStrategy
{
    /// <summary>
    /// Group by experiment key (service type name).
    /// </summary>
    ByExperimentKey,

    /// <summary>
    /// Group by subject ID.
    /// </summary>
    BySubjectId,

    /// <summary>
    /// Group by tenant ID.
    /// </summary>
    ByTenantId
}
