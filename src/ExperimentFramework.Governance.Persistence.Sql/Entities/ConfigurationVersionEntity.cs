namespace ExperimentFramework.Governance.Persistence.Sql.Entities;

/// <summary>
/// EF Core entity for configuration versions (immutable/append-only).
/// </summary>
public sealed class ConfigurationVersionEntity
{
    public long Id { get; set; }
    public required string ExperimentName { get; set; }
    public required int VersionNumber { get; set; }
    public required string ConfigurationJson { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? ChangeDescription { get; set; }
    public int? LifecycleState { get; set; }
    public required string ConfigurationHash { get; set; }
    public bool IsRollback { get; set; }
    public int? RolledBackFrom { get; set; }
    public string? MetadataJson { get; set; }
    public string? TenantId { get; set; }
    public string? Environment { get; set; }
}
