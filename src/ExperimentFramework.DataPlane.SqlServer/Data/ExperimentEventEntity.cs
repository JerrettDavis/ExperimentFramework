using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExperimentFramework.DataPlane.SqlServer.Data;

[Table("ExperimentEvents")]
public sealed class ExperimentEventEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [MaxLength(100)]
    public required string EventId { get; set; }

    [Required]
    public required DateTimeOffset Timestamp { get; set; }

    [Required]
    [MaxLength(50)]
    public required string EventType { get; set; }

    [Required]
    [MaxLength(20)]
    public required string SchemaVersion { get; set; }

    [Required]
    public required string PayloadJson { get; set; }

    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    public string? MetadataJson { get; set; }

    [Required]
    public DateTimeOffset CreatedAt { get; set; }
}
