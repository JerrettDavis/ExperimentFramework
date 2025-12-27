namespace ExperimentFramework.Configuration.Validation;

/// <summary>
/// Result of validating experiment configuration.
/// </summary>
/// <param name="errors">The validation errors and warnings.</param>
public sealed class ConfigurationValidationResult(IEnumerable<ConfigurationValidationError> errors)
{
    /// <summary>
    /// Whether the configuration is valid (no errors, warnings are allowed).
    /// </summary>
    public bool IsValid => !Errors.Exists(e => e.Severity == ValidationSeverity.Error);

    /// <summary>
    /// All validation errors and warnings.
    /// </summary>
    public List<ConfigurationValidationError> Errors { get; } = [.. errors];

    /// <summary>
    /// Only the warnings (non-fatal issues).
    /// </summary>
    public IEnumerable<ConfigurationValidationError> Warnings =>
        Errors.Where(e => e.Severity == ValidationSeverity.Warning);

    /// <summary>
    /// Only the errors (fatal issues that prevent configuration from loading).
    /// </summary>
    public IEnumerable<ConfigurationValidationError> FatalErrors =>
        Errors.Where(e => e.Severity == ValidationSeverity.Error);

    /// <summary>
    /// Creates a successful validation result with no errors.
    /// </summary>
    public static ConfigurationValidationResult Success { get; } = new([]);
}

/// <summary>
/// A single validation error or warning.
/// </summary>
/// <param name="Path">The path to the configuration element that has the error.</param>
/// <param name="Message">The error message.</param>
/// <param name="Severity">The severity of the error.</param>
public sealed record ConfigurationValidationError(string Path, string Message, ValidationSeverity Severity)
{
    /// <summary>
    /// Creates an error (fatal).
    /// </summary>
    public static ConfigurationValidationError Error(string path, string message) =>
        new(path, message, ValidationSeverity.Error);

    /// <summary>
    /// Creates a warning (non-fatal).
    /// </summary>
    public static ConfigurationValidationError Warning(string path, string message) =>
        new(path, message, ValidationSeverity.Warning);

    /// <inheritdoc />
    public override string ToString() => $"[{Severity}] {Path}: {Message}";
}

/// <summary>
/// Severity level for validation issues.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Non-fatal issue that should be logged but doesn't prevent loading.
    /// </summary>
    Warning,

    /// <summary>
    /// Fatal issue that prevents the configuration from being loaded.
    /// </summary>
    Error
}
