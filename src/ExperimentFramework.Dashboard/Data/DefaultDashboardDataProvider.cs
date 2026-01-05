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

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDashboardDataProvider"/> class.
    /// </summary>
    /// <param name="registry">The experiment registry.</param>
    public DefaultDashboardDataProvider(IExperimentRegistry? registry = null)
    {
        _registry = registry;
    }

    /// <inheritdoc />
    public Task<IEnumerable<DashboardExperimentInfo>> GetExperimentsAsync(string? tenantId, CancellationToken cancellationToken = default)
    {
        if (_registry == null)
        {
            return Task.FromResult<IEnumerable<DashboardExperimentInfo>>(Array.Empty<DashboardExperimentInfo>());
        }

        var experiments = _registry.GetAllExperiments()
            .Select(e => new DashboardExperimentInfo
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
                }).ToList()
            });

        // Note: Tenant filtering would require additional metadata in the experiment registry
        // For now, we return all experiments. Implementers can override this for tenant-specific filtering.

        return Task.FromResult(experiments);
    }

    /// <inheritdoc />
    public Task<DashboardExperimentInfo?> GetExperimentAsync(string name, string? tenantId, CancellationToken cancellationToken = default)
    {
        if (_registry == null)
        {
            return Task.FromResult<DashboardExperimentInfo?>(null);
        }

        var experiment = _registry.GetExperiment(name);
        if (experiment == null)
        {
            return Task.FromResult<DashboardExperimentInfo?>(null);
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
            }).ToList()
        };

        return Task.FromResult<DashboardExperimentInfo?>(info);
    }
}
