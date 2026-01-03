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
        var hasDsl = _dslManager.GetLastApplied().lastYaml is not null;
        var plugins = _pluginState.GetAllPlugins();

        return
        [
            new FeatureInfo(
                "Experiment Targeting",
                enabled: true,
                description: "Context-aware routing via ExperimentFramework.Targeting",
                category: "Routing"),
            new FeatureInfo(
                "Audit Logging",
                enabled: true,
                description: "Structured audit trail for experiment changes and selections",
                category: "Observability"),
            new FeatureInfo(
                "DSL Authoring",
                enabled: true,
                description: "Validate and apply YAML DSL at runtime",
                category: "Configuration"),
            new FeatureInfo(
                "DSL Export",
                enabled: true,
                description: "Export the current graph as YAML configuration",
                category: "Configuration"),
            new FeatureInfo(
                "Decorators & Proxies",
                enabled: true,
                description: "Runtime proxies with decorator pipeline",
                category: "Runtime"),
            new FeatureInfo(
                "Timeout Enforcement",
                enabled: true,
                description: "Automatic timeout handling with fallback",
                category: "Resilience"),
            new FeatureInfo(
                "Kill Switch",
                enabled: IsKillSwitchActive(),
                description: "Disable experiments or trials without redeploying",
                category: "Resilience"),
            new FeatureInfo(
                "Selection: Configuration",
                enabled: true,
                description: "Variants selected via configuration keys",
                category: "Selection"),
            new FeatureInfo(
                "Selection: Feature Flags",
                enabled: _experimentState.SupportsFeatureFlags,
                description: "Boolean feature flags for routing",
                category: "Selection"),
            new FeatureInfo(
                "Selection: Custom Providers",
                enabled: _experimentState.SupportsCustomProviders,
                description: "Pluggable provider model for bespoke routing",
                category: "Selection"),
            new FeatureInfo(
                "Plugin System",
                enabled: plugins.Any(),
                description: "Hot-reloadable plugin loading and activation",
                category: "Extensibility"),
            new FeatureInfo(
                "Analytics & Usage",
                enabled: _experimentState.HasUsage,
                description: "Aggregated usage tracking for experiments",
                category: "Observability"),
            new FeatureInfo(
                "DSL Applied",
                enabled: hasDsl,
                description: hasDsl ? "Latest YAML has been applied" : "Waiting for first DSL apply",
                category: "Configuration"),
            new FeatureInfo(
                "Runtime Configuration",
                enabled: true,
                description: "Live configuration changes without restart",
                category: "Operations")
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
