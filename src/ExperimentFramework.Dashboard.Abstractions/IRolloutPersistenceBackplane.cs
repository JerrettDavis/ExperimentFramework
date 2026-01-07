namespace ExperimentFramework.Dashboard.Abstractions;

/// <summary>
/// Provides persistence for rollout configurations.
/// </summary>
public interface IRolloutPersistenceBackplane
{
    /// <summary>
    /// Gets the rollout configuration for a specific experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="tenantId">Optional tenant ID for multi-tenancy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rollout configuration, or null if not found.</returns>
    Task<RolloutConfiguration?> GetRolloutConfigAsync(
        string experimentName,
        string? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves or updates a rollout configuration.
    /// </summary>
    /// <param name="config">The rollout configuration to save.</param>
    /// <param name="tenantId">Optional tenant ID for multi-tenancy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveRolloutConfigAsync(
        RolloutConfiguration config,
        string? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a rollout configuration.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="tenantId">Optional tenant ID for multi-tenancy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteRolloutConfigAsync(
        string experimentName,
        string? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active rollouts.
    /// </summary>
    /// <param name="tenantId">Optional tenant ID for multi-tenancy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All active rollout configurations.</returns>
    Task<IReadOnlyList<RolloutConfiguration>> GetActiveRolloutsAsync(
        string? tenantId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Rollout configuration model.
/// </summary>
public sealed class RolloutConfiguration
{
    /// <summary>
    /// Gets or sets the experiment name.
    /// </summary>
    public string ExperimentName { get; set; } = "";

    /// <summary>
    /// Gets or sets whether the rollout is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the target variant to roll out.
    /// </summary>
    public string? TargetVariant { get; set; }

    /// <summary>
    /// Gets or sets the current rollout percentage (0-100).
    /// </summary>
    public int Percentage { get; set; } = 0;

    /// <summary>
    /// Gets or sets the rollout stages.
    /// </summary>
    public List<RolloutStageDto> Stages { get; set; } = [];

    /// <summary>
    /// Gets or sets the rollout start date.
    /// </summary>
    public DateTimeOffset? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the rollout status.
    /// </summary>
    public RolloutStatus Status { get; set; } = RolloutStatus.NotStarted;

    /// <summary>
    /// Gets or sets the estimated total users.
    /// </summary>
    public int TotalUsers { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the number of users currently in the rollout.
    /// </summary>
    public int UsersInRollout { get; set; } = 0;

    /// <summary>
    /// Gets or sets the last modified timestamp.
    /// </summary>
    public DateTimeOffset LastModified { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public string? TenantId { get; set; }
}

/// <summary>
/// Rollout stage information.
/// </summary>
public sealed class RolloutStageDto
{
    /// <summary>
    /// Gets or sets the stage name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the stage description.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Gets or sets the target percentage for this stage (0-100).
    /// </summary>
    public int Percentage { get; set; }

    /// <summary>
    /// Gets or sets the scheduled date for this stage.
    /// </summary>
    public DateTimeOffset? ScheduledDate { get; set; }

    /// <summary>
    /// Gets or sets the actual execution date.
    /// </summary>
    public DateTimeOffset? ExecutedDate { get; set; }

    /// <summary>
    /// Gets or sets the stage status.
    /// </summary>
    public RolloutStageStatus Status { get; set; } = RolloutStageStatus.Pending;

    /// <summary>
    /// Gets or sets the planned duration in hours.
    /// </summary>
    public int? DurationHours { get; set; }

    /// <summary>
    /// Gets or sets the number of users affected by this stage.
    /// </summary>
    public int UsersAffected { get; set; } = 0;

    /// <summary>
    /// Gets or sets metrics collected during this stage.
    /// </summary>
    public Dictionary<string, double> Metrics { get; set; } = [];
}

/// <summary>
/// Rollout status enumeration.
/// </summary>
public enum RolloutStatus
{
    /// <summary>
    /// Rollout has not started yet.
    /// </summary>
    NotStarted,

    /// <summary>
    /// Rollout is in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Rollout has completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Rollout is paused.
    /// </summary>
    Paused,

    /// <summary>
    /// Rollout has been rolled back.
    /// </summary>
    RolledBack
}

/// <summary>
/// Rollout stage status enumeration.
/// </summary>
public enum RolloutStageStatus
{
    /// <summary>
    /// Stage is pending execution.
    /// </summary>
    Pending,

    /// <summary>
    /// Stage is currently active.
    /// </summary>
    Active,

    /// <summary>
    /// Stage has been completed.
    /// </summary>
    Completed,

    /// <summary>
    /// Stage was skipped.
    /// </summary>
    Skipped
}
