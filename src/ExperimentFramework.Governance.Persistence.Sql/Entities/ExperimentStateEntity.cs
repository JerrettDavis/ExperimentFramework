using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExperimentFramework.Governance.Persistence.Sql.Entities;

/// <summary>
/// EF Core entity for experiment state.
/// </summary>
[Table("ExperimentStates")]
public sealed class ExperimentStateEntity
{
    [Key]
    [Column(Order = 0)]
    [MaxLength(200)]
    public required string ExperimentName { get; set; }

    [Key]
    [Column(Order = 1)]
    [MaxLength(100)]
    public string TenantId { get; set; } = string.Empty;

    [Key]
    [Column(Order = 2)]
    [MaxLength(100)]
    public string Environment { get; set; } = string.Empty;

    [Required]
    public required int CurrentState { get; set; }

    public int ConfigurationVersion { get; set; }

    [Required]
    public required DateTimeOffset LastModified { get; set; }

    [MaxLength(200)]
    public string? LastModifiedBy { get; set; }

    [Required]
    [MaxLength(100)]
    [ConcurrencyCheck]
    public required string ETag { get; set; }

    public string? MetadataJson { get; set; }
}
