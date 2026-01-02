namespace ExperimentFramework.ServiceRegistration;

/// <summary>
/// Represents an ordered list of patch operations with validation results.
/// </summary>
/// <remarks>
/// <para>
/// A registration plan is the central artifact of the DI mutation safety system.
/// It provides:
/// </para>
/// <list type="bullet">
/// <item><description>A deterministic, repeatable sequence of mutations</description></item>
/// <item><description>Validation results before execution</description></item>
/// <item><description>Audit trail of all changes</description></item>
/// <item><description>Rollback capability if execution fails</description></item>
/// </list>
/// </remarks>
public sealed class RegistrationPlan
{
    /// <summary>
    /// Gets the unique identifier for this plan.
    /// </summary>
    public string PlanId { get; }

    /// <summary>
    /// Gets the snapshot of the service graph before mutations.
    /// </summary>
    public ServiceGraphSnapshot Snapshot { get; }

    /// <summary>
    /// Gets the ordered list of patch operations to apply.
    /// </summary>
    public IReadOnlyList<ServiceGraphPatchOperation> Operations { get; }

    /// <summary>
    /// Gets the validation findings discovered during plan validation.
    /// </summary>
    public IReadOnlyList<ValidationFinding> Findings { get; }

    /// <summary>
    /// Gets a value indicating whether the plan is valid and can be executed.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the timestamp when this plan was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets the validation mode used when validating this plan.
    /// </summary>
    public ValidationMode ValidationMode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegistrationPlan"/> class.
    /// </summary>
    public RegistrationPlan(
        string planId,
        ServiceGraphSnapshot snapshot,
        IReadOnlyList<ServiceGraphPatchOperation> operations,
        IReadOnlyList<ValidationFinding> findings,
        bool isValid,
        ValidationMode validationMode)
    {
        PlanId = planId ?? throw new ArgumentNullException(nameof(planId));
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        Operations = operations ?? throw new ArgumentNullException(nameof(operations));
        Findings = findings ?? throw new ArgumentNullException(nameof(findings));
        IsValid = isValid;
        CreatedAt = DateTimeOffset.UtcNow;
        ValidationMode = validationMode;
    }

    /// <summary>
    /// Gets a value indicating whether this plan has any error-level findings.
    /// </summary>
    public bool HasErrors => Findings.Any(f => f.Severity == ValidationSeverity.Error);

    /// <summary>
    /// Gets a value indicating whether this plan has any warning-level findings.
    /// </summary>
    public bool HasWarnings => Findings.Any(f => f.Severity == ValidationSeverity.Warning);

    /// <summary>
    /// Gets the count of error-level findings.
    /// </summary>
    public int ErrorCount => Findings.Count(f => f.Severity == ValidationSeverity.Error);

    /// <summary>
    /// Gets the count of warning-level findings.
    /// </summary>
    public int WarningCount => Findings.Count(f => f.Severity == ValidationSeverity.Warning);
}
