namespace ExperimentFramework.Configuration.Models;

/// <summary>
/// Configuration for a condition (implementation variant).
/// </summary>
public sealed class ConditionConfig
{
    /// <summary>
    /// The key identifying this condition.
    /// This is used for selection and fallback references.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// The implementation type name.
    /// Can be simple ("MyService") or fully qualified ("MyApp.Services.MyService, MyApp").
    /// </summary>
    public required string ImplementationType { get; set; }
}
