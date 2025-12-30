using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ExperimentFramework.Governance.Persistence.Sql.Entities;

/// <summary>
/// EF Core entity for state transitions (immutable/append-only).
/// </summary>
[Table("StateTransitions")]
[Index(nameof(ExperimentName), nameof(TenantId), nameof(Environment), nameof(Timestamp))]
public sealed class StateTransitionEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [MaxLength(100)]
    public required string TransitionId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string ExperimentName { get; set; }

    [Required]
    public required int FromState { get; set; }

    [Required]
    public required int ToState { get; set; }

    [Required]
    public required DateTimeOffset Timestamp { get; set; }

    [MaxLength(200)]
    public string? Actor { get; set; }

    public string? Reason { get; set; }

    public string? MetadataJson { get; set; }

    [MaxLength(100)]
    public string? TenantId { get; set; }

    [MaxLength(100)]
    public string? Environment { get; set; }
}
