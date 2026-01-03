namespace ExperimentFramework.Governance.Persistence.Sql.Entities;

/// <summary>
/// EF Core entity for policy evaluations (immutable/append-only).
/// </summary>
public sealed class PolicyEvaluationEntity
{
    public long Id { get; set; }
    public required string EvaluationId { get; set; }
    public required string ExperimentName { get; set; }
    public required string PolicyName { get; set; }
    public required bool IsCompliant { get; set; }
    public string? Reason { get; set; }
    public required int Severity { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
    public int? CurrentState { get; set; }
    public int? TargetState { get; set; }
    public string? MetadataJson { get; set; }
    public string? TenantId { get; set; }
    public string? Environment { get; set; }
}
