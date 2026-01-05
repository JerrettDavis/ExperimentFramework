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
                ServiceType = e.ServiceType?.Name,
                IsActive = e.IsActive,
                TrialCount = e.Trials?.Count ?? 0,
                Trials = e.Trials?.Select(t => new DashboardTrialInfo
                {
                    Key = t.Key,
                    ImplementationType = t.ImplementationType?.Name,
                    IsControl = t.IsControl
                }).ToList(),
                Rollout = rollout
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
            ServiceType = experiment.ServiceType?.Name,
            IsActive = experiment.IsActive,
            TrialCount = experiment.Trials?.Count ?? 0,
            Trials = experiment.Trials?.Select(t => new DashboardTrialInfo
            {
                Key = t.Key,
                ImplementationType = t.ImplementationType?.Name,
                IsControl = t.IsControl
            }).ToList(),
            Rollout = rollout
        };

        return info;
    }
}
