using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExperimentFramework.Governance.Persistence.Sql.Entities;

/// <summary>
/// EF Core entity for approval records (immutable/append-only).
/// </summary>
[Table("ApprovalRecords")]
[Index(nameof(ExperimentName), nameof(TenantId), nameof(Environment), nameof(Timestamp))]
[Index(nameof(TransitionId))]
public sealed class ApprovalRecordEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [MaxLength(100)]
    public required string ApprovalId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string ExperimentName { get; set; }

    [Required]
    [MaxLength(100)]
    public required string TransitionId { get; set; }

    public int? FromState { get; set; }

    [Required]
    public required int ToState { get; set; }

    [Required]
    public required bool IsApproved { get; set; }

    [MaxLength(200)]
    public string? Approver { get; set; }

    public string? Reason { get; set; }

    [Required]
    public required DateTimeOffset Timestamp { get; set; }

    [Required]
    [MaxLength(200)]
    public required string GateName { get; set; }

    public string? MetadataJson { get; set; }

    [MaxLength(100)]
    public string? TenantId { get; set; }

    [MaxLength(100)]
    public string? Environment { get; set; }
}
