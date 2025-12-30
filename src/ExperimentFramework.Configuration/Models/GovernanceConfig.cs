namespace ExperimentFramework.Configuration.Models;

/// <summary>
/// Configuration for experiment governance features.
/// </summary>
public sealed class GovernanceConfig
{
    /// <summary>
    /// Initial lifecycle state for experiments (default: Draft).
    /// </summary>
    public string? InitialState { get; set; }

    /// <summary>
    /// Approval gates for lifecycle transitions.
    /// </summary>
    public List<ApprovalGateConfig>? ApprovalGates { get; set; }

    /// <summary>
    /// Policy guardrails for experiments.
    /// </summary>
    public List<PolicyConfig>? Policies { get; set; }

    /// <summary>
    /// Whether to enable automatic versioning on configuration changes.
    /// </summary>
    public bool? EnableAutoVersioning { get; set; }

    /// <summary>
    /// Persistence configuration for durable governance state.
    /// </summary>
    public PersistenceConfig? Persistence { get; set; }
}

/// <summary>
/// Configuration for an approval gate.
/// </summary>
public sealed class ApprovalGateConfig
{
    /// <summary>
    /// Type of approval gate (automatic, manual, roleBased, or custom type name).
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Source lifecycle state (null for any state).
    /// </summary>
    public string? FromState { get; set; }

    /// <summary>
    /// Target lifecycle state.
    /// </summary>
    public required string ToState { get; set; }

    /// <summary>
    /// Allowed roles (for roleBased type).
    /// </summary>
    public List<string>? AllowedRoles { get; set; }

    /// <summary>
    /// Additional configuration properties for custom gates.
    /// </summary>
    public Dictionary<string, object>? Properties { get; set; }
}

/// <summary>
/// Configuration for a policy guardrail.
/// </summary>
public sealed class PolicyConfig
{
    /// <summary>
    /// Type of policy (trafficLimit, errorRate, timeWindow, conflictPrevention, or custom type name).
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Policy name (optional, defaults to type).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Maximum traffic percentage (for trafficLimit policy).
    /// </summary>
    public double? MaxTrafficPercentage { get; set; }

    /// <summary>
    /// Minimum stable time before exceeding traffic limit (for trafficLimit policy).
    /// </summary>
    public string? MinStableTime { get; set; }

    /// <summary>
    /// Maximum error rate threshold (for errorRate policy).
    /// </summary>
    public double? MaxErrorRate { get; set; }

    /// <summary>
    /// Start of allowed time window in HH:mm format (for timeWindow policy).
    /// </summary>
    public string? AllowedStartTime { get; set; }

    /// <summary>
    /// End of allowed time window in HH:mm format (for timeWindow policy).
    /// </summary>
    public string? AllowedEndTime { get; set; }

    /// <summary>
    /// List of conflicting experiment names (for conflictPrevention policy).
    /// </summary>
    public List<string>? ConflictingExperiments { get; set; }

    /// <summary>
    /// Additional configuration properties for custom policies.
    /// </summary>
    public Dictionary<string, object>? Properties { get; set; }
}

/// <summary>
/// Configuration for governance persistence backplane.
/// </summary>
public sealed class PersistenceConfig
{
    /// <summary>
    /// Type of persistence backplane (inMemory, sql, redis, or custom type name).
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Connection string (for SQL or Redis).
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Whether to enable optimistic concurrency control (default: true).
    /// </summary>
    public bool? OptimisticConcurrency { get; set; }

    /// <summary>
    /// Whether to retain full history (default: true).
    /// </summary>
    public bool? RetainHistory { get; set; }

    /// <summary>
    /// Key prefix for Redis keys (default: "governance:").
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Database provider for SQL (sqlserver, postgresql, sqlite, or custom).
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Additional configuration properties for custom persistence backplanes.
    /// </summary>
    public Dictionary<string, object>? Properties { get; set; }
}
