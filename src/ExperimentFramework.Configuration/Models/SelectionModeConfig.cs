namespace ExperimentFramework.Configuration.Models;

/// <summary>
/// Selection mode configuration determining how variants are selected.
/// </summary>
public sealed class SelectionModeConfig
{
    /// <summary>
    /// Selection mode type.
    /// Valid values: "featureFlag", "configurationKey", "variantFeatureFlag",
    /// "openFeature", "stickyRouting", "custom".
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Feature flag name (for "featureFlag" and "variantFeatureFlag" modes).
    /// If not specified, derived from the service type using naming convention.
    /// </summary>
    public string? FlagName { get; set; }

    /// <summary>
    /// Configuration key path (for "configurationKey" mode).
    /// If not specified, derived from the service type using naming convention.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// OpenFeature flag key (for "openFeature" mode).
    /// If not specified, derived from the service type using naming convention.
    /// </summary>
    public string? FlagKey { get; set; }

    /// <summary>
    /// Selector name (for "stickyRouting" and "custom" modes).
    /// </summary>
    public string? SelectorName { get; set; }

    /// <summary>
    /// Custom mode identifier (for "custom" mode).
    /// This identifies the registered custom selection mode provider.
    /// </summary>
    public string? ModeIdentifier { get; set; }

    /// <summary>
    /// Additional options for extension-provided selection modes.
    /// </summary>
    /// <remarks>
    /// Extension packages can use this property to configure mode-specific settings.
    /// For example, the Rollout package uses this for percentage configuration.
    /// </remarks>
    public Dictionary<string, object>? Options { get; set; }
}
