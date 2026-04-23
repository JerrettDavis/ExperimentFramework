namespace ExperimentFramework.Dashboard.Abstractions;

/// <summary>
/// Provides data access for the dashboard.
/// </summary>
/// <remarks>
/// Implementations can add caching, filtering, or other cross-cutting concerns.
/// The default implementation delegates to <c>IExperimentRegistry</c>.
/// </remarks>
public interface IDashboardDataProvider
{
    /// <summary>
    /// Gets all experiments for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier, or null for the default tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of experiment information.</returns>
    Task<IEnumerable<ExperimentInfo>> GetExperimentsAsync(string? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific experiment by name.
    /// </summary>
    /// <param name="name">The experiment name.</param>
    /// <param name="tenantId">The tenant identifier, or null for the default tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The experiment information, or null if not found.</returns>
    Task<ExperimentInfo?> GetExperimentAsync(string name, string? tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents experiment information for the dashboard.
/// </summary>
public sealed class ExperimentInfo
{
    /// <summary>
    /// Gets or sets the experiment name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the human-readable display name for the experiment.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets or sets the experiment description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the category used to group experiments in the UI
    /// (e.g. "Revenue", "Engagement", "UX").
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Gets or sets the service type name.
    /// </summary>
    public string? ServiceType { get; init; }

    /// <summary>
    /// Gets or sets whether the experiment is active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Gets or sets the currently active variant key, when applicable.
    /// </summary>
    public string? ActiveVariant { get; init; }

    /// <summary>
    /// Gets or sets the number of trials/variants.
    /// </summary>
    public int TrialCount { get; init; }

    /// <summary>
    /// Gets or sets the trial information.
    /// </summary>
    public IReadOnlyList<TrialInfo>? Trials { get; init; }

    /// <summary>
    /// Gets or sets the selection mode.
    /// </summary>
    public string? SelectionMode { get; init; }

    /// <summary>
    /// Gets or sets the rollout configuration for this experiment.
    /// </summary>
    public RolloutConfiguration? Rollout { get; init; }

    /// <summary>
    /// Gets or sets the UTC timestamp of the experiment's last modification.
    /// </summary>
    public DateTime LastModified { get; init; }
}

/// <summary>
/// Represents trial/variant information.
/// </summary>
public sealed class TrialInfo
{
    /// <summary>
    /// Gets or sets the trial key.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets or sets the implementation type name.
    /// </summary>
    public string? ImplementationType { get; init; }

    /// <summary>
    /// Gets or sets whether this is the control trial.
    /// </summary>
    public bool IsControl { get; init; }
}
