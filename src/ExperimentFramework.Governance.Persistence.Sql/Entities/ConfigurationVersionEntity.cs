using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExperimentFramework.Governance.Persistence.Sql.Entities;

/// <summary>
/// EF Core entity for configuration versions (immutable/append-only).
/// </summary>
[Table("ConfigurationVersions")]
[Index(nameof(ExperimentName), nameof(VersionNumber), nameof(TenantId), nameof(Environment), IsUnique = true)]
public sealed class ConfigurationVersionEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [MaxLength(200)]
    public required string ExperimentName { get; set; }

    [Required]
    public required int VersionNumber { get; set; }

    [Required]
    public required string ConfigurationJson { get; set; }

    [Required]
    public required DateTimeOffset CreatedAt { get; set; }

    [MaxLength(200)]
    public string? CreatedBy { get; set; }

    public string? ChangeDescription { get; set; }

    public int? LifecycleState { get; set; }

    [Required]
    [MaxLength(64)]
    public required string ConfigurationHash { get; set; }

    public bool IsRollback { get; set; }

    public int? RolledBackFrom { get; set; }

    public string? MetadataJson { get; set; }

    [MaxLength(100)]
    public string? TenantId { get; set; }

    [MaxLength(100)]
    public string? Environment { get; set; }
}
