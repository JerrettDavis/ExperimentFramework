using ExperimentFramework.Audit;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ExperimentFramework.Governance.Versioning;

/// <summary>
/// Manages experiment configuration versions.
/// </summary>
public interface IVersionManager
{
    /// <summary>
    /// Creates a new version of an experiment configuration.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="configuration">The configuration object.</param>
    /// <param name="actor">The actor creating the version.</param>
    /// <param name="changeDescription">Description of the changes.</param>
    /// <param name="lifecycleState">Optional lifecycle state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created version.</returns>
    Task<ExperimentVersion> CreateVersionAsync(
        string experimentName,
        object configuration,
        string? actor = null,
        string? changeDescription = null,
        ExperimentLifecycleState? lifecycleState = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific version of an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="versionNumber">The version number.</param>
    /// <returns>The experiment version, or null if not found.</returns>
    ExperimentVersion? GetVersion(string experimentName, int versionNumber);

    /// <summary>
    /// Gets the latest version of an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <returns>The latest version, or null if no versions exist.</returns>
    ExperimentVersion? GetLatestVersion(string experimentName);

    /// <summary>
    /// Gets all versions of an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <returns>All versions in chronological order.</returns>
    IReadOnlyList<ExperimentVersion> GetAllVersions(string experimentName);

    /// <summary>
    /// Computes the difference between two versions.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="fromVersion">The source version number.</param>
    /// <param name="toVersion">The target version number.</param>
    /// <returns>The version diff, or null if either version doesn't exist.</returns>
    VersionDiff? GetDiff(string experimentName, int fromVersion, int toVersion);

    /// <summary>
    /// Rolls back to a previous version by creating a new version with the old configuration.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="targetVersion">The version to roll back to.</param>
    /// <param name="actor">The actor performing the rollback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new version with rolled-back configuration.</returns>
    Task<ExperimentVersion> RollbackToVersionAsync(
        string experimentName,
        int targetVersion,
        string? actor = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Default in-memory implementation of version manager.
/// </summary>
public class VersionManager : IVersionManager
{
    private readonly ILogger<VersionManager> _logger;
    private readonly IAuditSink? _auditSink;
    private readonly Dictionary<string, List<ExperimentVersion>> _versions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionManager"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="auditSink">Optional audit sink.</param>
    public VersionManager(
        ILogger<VersionManager> logger,
        IAuditSink? auditSink = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditSink = auditSink;
    }

    /// <inheritdoc/>
    public async Task<ExperimentVersion> CreateVersionAsync(
        string experimentName,
        object configuration,
        string? actor = null,
        string? changeDescription = null,
        ExperimentLifecycleState? lifecycleState = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(experimentName))
            throw new ArgumentException("Experiment name cannot be null or empty.", nameof(experimentName));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        int versionNumber;
        lock (_versions)
        {
            if (!_versions.ContainsKey(experimentName))
            {
                _versions[experimentName] = new List<ExperimentVersion>();
            }

            versionNumber = _versions[experimentName].Count + 1;
        }

        var version = new ExperimentVersion
        {
            VersionNumber = versionNumber,
            ExperimentName = experimentName,
            Configuration = configuration,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = actor,
            ChangeDescription = changeDescription,
            LifecycleState = lifecycleState
        };

        lock (_versions)
        {
            _versions[experimentName].Add(version);
        }

        _logger.LogInformation(
            "Created version {VersionNumber} for experiment '{ExperimentName}' by {Actor}",
            versionNumber, experimentName, actor ?? "system");

        // Emit audit event
        if (_auditSink != null)
        {
            var auditEvent = new AuditEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Timestamp = version.CreatedAt,
                EventType = AuditEventType.ExperimentModified,
                ExperimentName = experimentName,
                Actor = actor,
                Details = new Dictionary<string, object>
                {
                    ["versionNumber"] = versionNumber,
                    ["changeDescription"] = changeDescription ?? "none"
                }
            };

            await _auditSink.RecordAsync(auditEvent, cancellationToken);
        }

        return version;
    }

    /// <inheritdoc/>
    public ExperimentVersion? GetVersion(string experimentName, int versionNumber)
    {
        if (string.IsNullOrWhiteSpace(experimentName))
            throw new ArgumentException("Experiment name cannot be null or empty.", nameof(experimentName));

        lock (_versions)
        {
            if (!_versions.TryGetValue(experimentName, out var versions))
                return null;

            return versions.FirstOrDefault(v => v.VersionNumber == versionNumber);
        }
    }

    /// <inheritdoc/>
    public ExperimentVersion? GetLatestVersion(string experimentName)
    {
        if (string.IsNullOrWhiteSpace(experimentName))
            throw new ArgumentException("Experiment name cannot be null or empty.", nameof(experimentName));

        lock (_versions)
        {
            if (!_versions.TryGetValue(experimentName, out var versions) || versions.Count == 0)
                return null;

            return versions[^1];
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<ExperimentVersion> GetAllVersions(string experimentName)
    {
        if (string.IsNullOrWhiteSpace(experimentName))
            throw new ArgumentException("Experiment name cannot be null or empty.", nameof(experimentName));

        lock (_versions)
        {
            if (!_versions.TryGetValue(experimentName, out var versions))
                return Array.Empty<ExperimentVersion>();

            return versions.ToList().AsReadOnly();
        }
    }

    /// <inheritdoc/>
    public VersionDiff? GetDiff(string experimentName, int fromVersion, int toVersion)
    {
        var from = GetVersion(experimentName, fromVersion);
        var to = GetVersion(experimentName, toVersion);

        if (from == null || to == null)
            return null;

        // Simple diff implementation using JSON comparison
        var changes = ComputeChanges(from.Configuration, to.Configuration);

        return new VersionDiff
        {
            FromVersion = fromVersion,
            ToVersion = toVersion,
            Changes = changes
        };
    }

    /// <inheritdoc/>
    public async Task<ExperimentVersion> RollbackToVersionAsync(
        string experimentName,
        int targetVersion,
        string? actor = null,
        CancellationToken cancellationToken = default)
    {
        var target = GetVersion(experimentName, targetVersion);
        if (target == null)
            throw new InvalidOperationException($"Version {targetVersion} not found for experiment '{experimentName}'");

        var description = $"Rolled back to version {targetVersion}";
        return await CreateVersionAsync(
            experimentName,
            target.Configuration,
            actor,
            description,
            target.LifecycleState,
            cancellationToken);
    }

    private static IReadOnlyList<ConfigurationChange> ComputeChanges(object from, object to)
    {
        // Simple implementation: serialize to JSON and compare
        // In production, you might use a more sophisticated diff algorithm
        var changes = new List<ConfigurationChange>();

        try
        {
            var fromJson = JsonSerializer.Serialize(from);
            var toJson = JsonSerializer.Serialize(to);

            if (fromJson != toJson)
            {
                changes.Add(new ConfigurationChange
                {
                    Type = ChangeType.Modified,
                    Path = "configuration",
                    OldValue = from,
                    NewValue = to
                });
            }
        }
        catch
        {
            // Fallback if serialization fails
            changes.Add(new ConfigurationChange
            {
                Type = ChangeType.Modified,
                Path = "configuration",
                OldValue = from,
                NewValue = to
            });
        }

        return changes.AsReadOnly();
    }
}
