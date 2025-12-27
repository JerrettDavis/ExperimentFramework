using ExperimentFramework.Configuration.Validation;

namespace ExperimentFramework.Configuration.Exceptions;

/// <summary>
/// Exception thrown when experiment configuration is invalid or cannot be loaded.
/// </summary>
public class ExperimentConfigurationException : Exception
{
    /// <summary>
    /// Validation errors that caused this exception.
    /// </summary>
    public IReadOnlyList<ConfigurationValidationError> ValidationErrors { get; }

    /// <summary>
    /// Creates a new instance with the specified message.
    /// </summary>
    public ExperimentConfigurationException(string message)
        : base(message)
    {
        ValidationErrors = [];
    }

    /// <summary>
    /// Creates a new instance with the specified message and inner exception.
    /// </summary>
    public ExperimentConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
        ValidationErrors = [];
    }

    /// <summary>
    /// Creates a new instance with validation errors.
    /// </summary>
    public ExperimentConfigurationException(
        string message,
        IEnumerable<ConfigurationValidationError> errors)
        : base(FormatMessage(message, errors))
    {
        ValidationErrors = errors.ToList().AsReadOnly();
    }

    private static string FormatMessage(string message, IEnumerable<ConfigurationValidationError> errors)
    {
        var errorList = errors.ToList();
        if (errorList.Count == 0)
            return message;

        var sb = new System.Text.StringBuilder(message);
        sb.AppendLine();
        sb.AppendLine("Validation errors:");

        foreach (var error in errorList.Where(e => e.Severity == ValidationSeverity.Error))
        {
            sb.AppendLine($"  - [{error.Path}] {error.Message}");
        }

        var warnings = errorList.Where(e => e.Severity == ValidationSeverity.Warning).ToList();
        if (warnings.Count > 0)
        {
            sb.AppendLine("Warnings:");
            foreach (var warning in warnings)
            {
                sb.AppendLine($"  - [{warning.Path}] {warning.Message}");
            }
        }

        return sb.ToString();
    }
}
