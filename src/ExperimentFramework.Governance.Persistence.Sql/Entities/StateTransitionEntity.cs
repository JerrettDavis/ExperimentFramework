namespace ExperimentFramework.Governance.Persistence.Sql.Entities;

/// <summary>
/// EF Core entity for state transitions (immutable/append-only).
/// </summary>
public sealed class StateTransitionEntity
{
    public long Id { get; set; }
    public required string TransitionId { get; set; }
    public required string ExperimentName { get; set; }
    public required int FromState { get; set; }
    public required int ToState { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
    public string? Actor { get; set; }
    public string? Reason { get; set; }
    public string? MetadataJson { get; set; }
    public string? TenantId { get; set; }
    public string? Environment { get; set; }
}
