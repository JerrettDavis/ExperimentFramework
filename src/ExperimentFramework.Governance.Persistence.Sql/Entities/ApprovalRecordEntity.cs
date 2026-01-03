namespace ExperimentFramework.Governance.Persistence.Sql.Entities;

/// <summary>
/// EF Core entity for approval records (immutable/append-only).
/// </summary>
public sealed class ApprovalRecordEntity
{
    public long Id { get; set; }
    public required string ApprovalId { get; set; }
    public required string ExperimentName { get; set; }
    public required string TransitionId { get; set; }
    public int? FromState { get; set; }
    public required int ToState { get; set; }
    public required bool IsApproved { get; set; }
    public string? Approver { get; set; }
    public string? Reason { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
    public required string GateName { get; set; }
    public string? MetadataJson { get; set; }
    public string? TenantId { get; set; }
    public string? Environment { get; set; }
}
