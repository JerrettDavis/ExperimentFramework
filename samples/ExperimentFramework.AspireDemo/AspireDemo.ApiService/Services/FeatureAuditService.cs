using AspireDemo.ApiService.Models;
using ExperimentFramework.KillSwitch;

namespace AspireDemo.ApiService.Services;

public sealed class FeatureAuditService
{
    private readonly RuntimeExperimentManager _dslManager;
    private readonly PluginStateManager _pluginState;
    private readonly ExperimentStateManager _experimentState;
    private readonly IKillSwitchProvider _killSwitch;

    public FeatureAuditService(
        RuntimeExperimentManager dslManager,
        PluginStateManager pluginState,
        ExperimentStateManager experimentState,
        IKillSwitchProvider killSwitch)
    {
        _dslManager = dslManager;
        _pluginState = pluginState;
        _experimentState = experimentState;
        _killSwitch = killSwitch;
    }

    public IReadOnlyList<FeatureInfo> GetFeatures()
    {
        var hasDsl = _dslManager.GetLastApplied().yaml is not null;
        var plugins = _pluginState.GetAllPlugins();

        return
        [
            new FeatureInfo(
                "Experiment Targeting",
                Enabled: true,
                Description: "Context-aware routing via ExperimentFramework.Targeting",
                Category: "Routing"),
            new FeatureInfo(
                "Audit Logging",
                Enabled: true,
                Description: "Structured audit trail for experiment changes and selections",
                Category: "Observability"),
            new FeatureInfo(
                "DSL Authoring",
                Enabled: true,
                Description: "Validate and apply YAML DSL at runtime",
                Category: "Configuration"),
            new FeatureInfo(
                "DSL Export",
                Enabled: true,
                Description: "Export the current graph as YAML configuration",
                Category: "Configuration"),
            new FeatureInfo(
                "Decorators & Proxies",
                Enabled: true,
                Description: "Runtime proxies with decorator pipeline",
                Category: "Runtime"),
            new FeatureInfo(
                "Timeout Enforcement",
                Enabled: true,
                Description: "Automatic timeout handling with fallback",
                Category: "Resilience"),
            new FeatureInfo(
                "Kill Switch",
                Enabled: IsKillSwitchActive(),
                Description: "Disable experiments or trials without redeploying",
                Category: "Resilience"),
            new FeatureInfo(
                "Selection: Configuration",
                Enabled: true,
                Description: "Variants selected via configuration keys",
                Category: "Selection"),
            new FeatureInfo(
                "Selection: Feature Flags",
                Enabled: _experimentState.SupportsFeatureFlags,
                Description: "Boolean feature flags for routing",
                Category: "Selection"),
            new FeatureInfo(
                "Selection: Custom Providers",
                Enabled: _experimentState.SupportsCustomProviders,
                Description: "Pluggable provider model for bespoke routing",
                Category: "Selection"),
            new FeatureInfo(
                "Plugin System",
                Enabled: plugins.Any(),
                Description: "Hot-reloadable plugin loading and activation",
                Category: "Extensibility"),
            new FeatureInfo(
                "Analytics & Usage",
                Enabled: _experimentState.HasUsage,
                Description: "Aggregated usage tracking for experiments",
                Category: "Observability"),
            new FeatureInfo(
                "DSL Applied",
                Enabled: hasDsl,
                Description: hasDsl ? "Latest YAML has been applied" : "Waiting for first DSL apply",
                Category: "Configuration"),
            new FeatureInfo(
                "Runtime Configuration",
                Enabled: true,
                Description: "Live configuration changes without restart",
                Category: "Operations")
        ];
    }

    private bool IsKillSwitchActive()
    {
        // If any experiment or variant is disabled, the kill switch is actively being used.
        foreach (var experiment in _experimentState.GetAllExperiments())
        {
            var type = ExperimentTypeResolver.GetServiceType(experiment.Name);
            if (_killSwitch.IsExperimentDisabled(type))
            {
                return true;
            }

            if (experiment.Variants.Any(v => _killSwitch.IsTrialDisabled(type, v.Name)))
            {
                return true;
            }
        }

        // Kill switch is wired up even if unused yet.
        return true;
    }
}
