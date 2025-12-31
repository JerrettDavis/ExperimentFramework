namespace ExperimentFramework.ServiceRegistration;

/// <summary>
/// Represents a validation issue discovered during registration plan validation.
/// </summary>
public sealed class ValidationFinding
{
    /// <summary>
    /// Gets the severity level of this finding.
    /// </summary>
    public ValidationSeverity Severity { get; }

    /// <summary>
    /// Gets the validation rule that was violated.
    /// </summary>
    public string RuleName { get; }

    /// <summary>
    /// Gets the service type associated with this finding.
    /// </summary>
    public Type ServiceType { get; }

    /// <summary>
    /// Gets the human-readable description of the issue.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the recommended action to resolve this issue.
    /// </summary>
    public string? RecommendedAction { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationFinding"/> class.
    /// </summary>
    public ValidationFinding(
        ValidationSeverity severity,
        string ruleName,
        Type serviceType,
        string description,
        string? recommendedAction = null)
    {
        Severity = severity;
        RuleName = ruleName ?? throw new ArgumentNullException(nameof(ruleName));
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        RecommendedAction = recommendedAction;
    }

    /// <summary>
    /// Creates an error finding.
    /// </summary>
    public static ValidationFinding Error(string ruleName, Type serviceType, string description, string? recommendedAction = null)
        => new(ValidationSeverity.Error, ruleName, serviceType, description, recommendedAction);

    /// <summary>
    /// Creates a warning finding.
    /// </summary>
    public static ValidationFinding Warning(string ruleName, Type serviceType, string description, string? recommendedAction = null)
        => new(ValidationSeverity.Warning, ruleName, serviceType, description, recommendedAction);

    /// <summary>
    /// Creates an info finding.
    /// </summary>
    public static ValidationFinding Info(string ruleName, Type serviceType, string description, string? recommendedAction = null)
        => new(ValidationSeverity.Info, ruleName, serviceType, description, recommendedAction);
}

/// <summary>
/// Defines the severity level of a validation finding.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational finding that does not indicate a problem.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning that should be reviewed but does not block execution.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error that must be resolved before execution can proceed safely.
    /// </summary>
    Error = 2
}
