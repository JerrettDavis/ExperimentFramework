namespace AspireDemo.ApiService.Data;

/// <summary>
/// Entity representing an experiment configuration.
/// </summary>
public class ExperimentEntity
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public string DefaultVariant { get; set; } = "default";
    public string ActiveVariant { get; set; } = "default";
    public string Source { get; set; } = "Code";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    public List<VariantEntity> Variants { get; set; } = [];
}

/// <summary>
/// Entity representing a variant within an experiment.
/// </summary>
public class VariantEntity
{
    public int Id { get; set; }
    public string ExperimentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ExperimentEntity? Experiment { get; set; }
}

/// <summary>
/// Entity representing a kill switch state.
/// </summary>
public class KillSwitchEntity
{
    public int Id { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public string? TrialKey { get; set; }
    public bool IsDisabled { get; set; }
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Entity representing an active plugin implementation.
/// </summary>
public class PluginImplementationEntity
{
    public int Id { get; set; }
    public string InterfaceName { get; set; } = string.Empty;
    public string PluginId { get; set; } = string.Empty;
    public string ImplementationType { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Entity representing a DSL configuration.
/// </summary>
public class DslConfigurationEntity
{
    public int Id { get; set; }
    public string YamlContent { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public bool IsCurrent { get; set; } = true;
}

/// <summary>
/// Entity representing an audit event.
/// </summary>
public class AuditEventEntity
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty;
    public string? ExperimentName { get; set; }
    public string? TrialName { get; set; }
    public string Details { get; set; } = string.Empty;
}
