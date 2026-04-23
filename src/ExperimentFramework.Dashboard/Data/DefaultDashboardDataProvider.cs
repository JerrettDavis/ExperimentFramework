using ExperimentFramework.Admin;
using ExperimentFramework.Dashboard.Abstractions;
using DashboardExperimentInfo = ExperimentFramework.Dashboard.Abstractions.ExperimentInfo;
using DashboardTrialInfo = ExperimentFramework.Dashboard.Abstractions.TrialInfo;

namespace ExperimentFramework.Dashboard.Data;

/// <summary>
/// Default dashboard data provider that delegates to IExperimentRegistry.
/// </summary>
public sealed class DefaultDashboardDataProvider : IDashboardDataProvider
{
    private readonly IExperimentRegistry? _registry;
    private readonly IRolloutPersistenceBackplane? _rolloutPersistence;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDashboardDataProvider"/> class.
    /// </summary>
    /// <param name="registry">The experiment registry.</param>
    /// <param name="rolloutPersistence">Optional rollout persistence provider.</param>
    public DefaultDashboardDataProvider(
        IExperimentRegistry? registry = null,
        IRolloutPersistenceBackplane? rolloutPersistence = null)
    {
        _registry = registry;
        _rolloutPersistence = rolloutPersistence;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DashboardExperimentInfo>> GetExperimentsAsync(string? tenantId, CancellationToken cancellationToken = default)
    {
        if (_registry == null)
        {
            return Array.Empty<DashboardExperimentInfo>();
        }

        var experiments = new List<DashboardExperimentInfo>();

        foreach (var e in _registry.GetAllExperiments())
        {
            RolloutConfiguration? rollout = null;
            if (_rolloutPersistence != null)
            {
                rollout = await _rolloutPersistence.GetRolloutConfigAsync(e.Name, tenantId, cancellationToken);
                if (rollout != null)
                {
                    rollout.ExperimentName = e.Name;
                }
            }

            experiments.Add(new DashboardExperimentInfo
            {
                Name = e.Name,
                DisplayName = GetMetadataString(e.Metadata, "DisplayName"),
                Description = GetMetadataString(e.Metadata, "Description"),
                Category = GetMetadataString(e.Metadata, "Category"),
                ServiceType = e.ServiceType?.Name,
                IsActive = e.IsActive,
                ActiveVariant = GetMetadataString(e.Metadata, "ActiveVariant"),
                TrialCount = e.Trials?.Count ?? 0,
                Trials = e.Trials?.Select(t => new DashboardTrialInfo
                {
                    Key = t.Key,
                    ImplementationType = t.ImplementationType?.Name,
                    IsControl = t.IsControl
                }).ToList(),
                Rollout = rollout,
                LastModified = GetMetadataDate(e.Metadata, "LastModified")
            });
        }

        // Note: Tenant filtering would require additional metadata in the experiment registry
        // For now, we return all experiments. Implementers can override this for tenant-specific filtering.

        return experiments;
    }

    /// <inheritdoc />
    public async Task<DashboardExperimentInfo?> GetExperimentAsync(string name, string? tenantId, CancellationToken cancellationToken = default)
    {
        if (_registry == null)
        {
            return null;
        }

        var experiment = _registry.GetExperiment(name);
        if (experiment == null)
        {
            return null;
        }

        RolloutConfiguration? rollout = null;
        if (_rolloutPersistence != null)
        {
            rollout = await _rolloutPersistence.GetRolloutConfigAsync(name, tenantId, cancellationToken);
            if (rollout != null)
            {
                rollout.ExperimentName = name;
            }
        }

        var info = new DashboardExperimentInfo
        {
            Name = experiment.Name,
            DisplayName = GetMetadataString(experiment.Metadata, "DisplayName"),
            Description = GetMetadataString(experiment.Metadata, "Description"),
            Category = GetMetadataString(experiment.Metadata, "Category"),
            ServiceType = experiment.ServiceType?.Name,
            IsActive = experiment.IsActive,
            ActiveVariant = GetMetadataString(experiment.Metadata, "ActiveVariant"),
            TrialCount = experiment.Trials?.Count ?? 0,
            Trials = experiment.Trials?.Select(t => new DashboardTrialInfo
            {
                Key = t.Key,
                ImplementationType = t.ImplementationType?.Name,
                IsControl = t.IsControl
            }).ToList(),
            Rollout = rollout,
            LastModified = GetMetadataDate(experiment.Metadata, "LastModified")
        };

        return info;
    }

    private static string? GetMetadataString(IReadOnlyDictionary<string, object>? metadata, string key)
    {
        if (metadata is null) return null;
        return metadata.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static DateTime GetMetadataDate(IReadOnlyDictionary<string, object>? metadata, string key)
    {
        if (metadata is null) return default;
        if (!metadata.TryGetValue(key, out var value)) return default;
        return value switch
        {
            DateTime dt => dt,
            DateTimeOffset dto => dto.UtcDateTime,
            string s when DateTime.TryParse(s, out var parsed) => parsed,
            _ => default
        };
    }
}
