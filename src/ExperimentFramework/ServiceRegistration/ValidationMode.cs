namespace ExperimentFramework.ServiceRegistration;

/// <summary>
/// Defines the validation strictness level for service registration mutations.
/// </summary>
/// <remarks>
/// <para>
/// This enum controls how the registration plan validator handles invariant violations
/// during DI container mutation.
/// </para>
/// </remarks>
public enum ValidationMode
{
    /// <summary>
    /// Validation is disabled. All mutations proceed without checks.
    /// </summary>
    /// <remarks>
    /// Use this mode only for advanced scenarios where you have external validation
    /// or need maximum performance. Not recommended for production use.
    /// </remarks>
    Off = 0,

    /// <summary>
    /// Violations produce warnings but do not fail startup.
    /// </summary>
    /// <remarks>
    /// This mode is useful during migration or when you want to collect validation
    /// findings without blocking deployment.
    /// </remarks>
    Warn = 1,

    /// <summary>
    /// Violations fail startup with a clear report (default for enterprise).
    /// </summary>
    /// <remarks>
    /// This is the recommended mode for production environments. Any unsafe condition
    /// will prevent application startup with a detailed report of the issue.
    /// </remarks>
    Strict = 2
}
