namespace ExperimentFramework.DataPlane.Abstractions.Events;

/// <summary>
/// Represents an exposure event: when a subject was exposed to a variant.
/// </summary>
/// <remarks>
/// <para>
/// Exposure is defined as <em>serving</em> a variant, not merely evaluating it.
/// This captures who was exposed, what variant they saw, when it occurred,
/// why the variant was selected, and how assignment consistency was determined.
/// </para>
/// </remarks>
public sealed class ExposureEvent
{
    /// <summary>
    /// Schema version for this event type.
    /// </summary>
    public const string SchemaVersion = "1.0.0";

    /// <summary>
    /// Gets or sets the experiment name.
    /// </summary>
    public required string ExperimentName { get; init; }

    /// <summary>
    /// Gets or sets the variant/trial key that was served.
    /// </summary>
    public required string VariantKey { get; init; }

    /// <summary>
    /// Gets or sets the subject identifier (user, session, device, etc.).
    /// </summary>
    public required string SubjectId { get; init; }

    /// <summary>
    /// Gets or sets the subject type (e.g., "user", "session", "device").
    /// </summary>
    public string? SubjectType { get; init; }

    /// <summary>
    /// Gets or sets the optional tenant identifier for multi-tenant scenarios.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets or sets the exposure timestamp.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets or sets the selection reason (e.g., "rule-match", "hash-assignment", "override", "fallback").
    /// </summary>
    public required string SelectionReason { get; init; }

    /// <summary>
    /// Gets or sets the rule path or selection logic that determined the variant.
    /// </summary>
    public string? RulePath { get; init; }

    /// <summary>
    /// Gets or sets the assignment policy used.
    /// </summary>
    public AssignmentPolicy AssignmentPolicy { get; init; }

    /// <summary>
    /// Gets or sets whether this is a repeated exposure for the same assignment.
    /// </summary>
    public bool IsRepeatExposure { get; init; }

    /// <summary>
    /// Gets or sets additional context or attributes.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Attributes { get; init; }
}
