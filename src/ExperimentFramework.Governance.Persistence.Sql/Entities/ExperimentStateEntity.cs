namespace ExperimentFramework.Governance.Persistence.Sql.Entities;

/// <summary>
/// EF Core entity for experiment state.
/// </summary>
public sealed class ExperimentStateEntity
{
    public required string ExperimentName { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public required int CurrentState { get; set; }
    public int ConfigurationVersion { get; set; }
    public required DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
    public required string ETag { get; set; }
    public string? MetadataJson { get; set; }
}
