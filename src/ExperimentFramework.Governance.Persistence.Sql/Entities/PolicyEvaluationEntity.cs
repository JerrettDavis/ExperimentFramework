using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExperimentFramework.Governance.Persistence.Sql.Entities;

/// <summary>
/// EF Core entity for policy evaluations (immutable/append-only).
/// </summary>
[Table("PolicyEvaluations")]
[Index(nameof(ExperimentName), nameof(TenantId), nameof(Environment), nameof(Timestamp))]
[Index(nameof(ExperimentName), nameof(PolicyName), nameof(Timestamp))]
public sealed class PolicyEvaluationEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [MaxLength(100)]
    public required string EvaluationId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string ExperimentName { get; set; }

    [Required]
    [MaxLength(200)]
    public required string PolicyName { get; set; }

    [Required]
    public required bool IsCompliant { get; set; }

    public string? Reason { get; set; }

    [Required]
    public required int Severity { get; set; }

    [Required]
    public required DateTimeOffset Timestamp { get; set; }

    public int? CurrentState { get; set; }

    public int? TargetState { get; set; }

    public string? MetadataJson { get; set; }

    [MaxLength(100)]
    public string? TenantId { get; set; }

    [MaxLength(100)]
    public string? Environment { get; set; }
}
