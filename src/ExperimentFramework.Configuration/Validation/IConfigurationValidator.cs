using ExperimentFramework.Configuration.Models;

namespace ExperimentFramework.Configuration.Validation;

/// <summary>
/// Validates experiment configuration for completeness and correctness.
/// </summary>
public interface IConfigurationValidator
{
    /// <summary>
    /// Validates the configuration root and returns any errors or warnings.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <returns>The validation result containing any errors or warnings.</returns>
    ConfigurationValidationResult Validate(ExperimentFrameworkConfigurationRoot config);
}
