namespace ExperimentFramework.Configuration.Models;

/// <summary>
/// Time-based and custom activation configuration.
/// </summary>
public sealed class ActivationConfig
{
    /// <summary>
    /// Activation start time (ISO 8601 format).
    /// The experiment/trial will not be active before this time.
    /// </summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>
    /// Activation end time (ISO 8601 format).
    /// The experiment/trial will not be active after this time.
    /// </summary>
    public DateTimeOffset? Until { get; set; }

    /// <summary>
    /// Custom activation predicate configuration.
    /// </summary>
    public PredicateConfig? Predicate { get; set; }
}

/// <summary>
/// Custom activation predicate configuration.
/// </summary>
public sealed class PredicateConfig
{
    /// <summary>
    /// Fully qualified type name of the predicate.
    /// Must implement <see cref="Activation.IActivationPredicate"/>.
    /// </summary>
    public required string Type { get; set; }
}
