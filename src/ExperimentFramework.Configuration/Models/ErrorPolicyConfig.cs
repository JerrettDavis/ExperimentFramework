namespace ExperimentFramework.Configuration.Models;

/// <summary>
/// Error handling policy configuration.
/// </summary>
public sealed class ErrorPolicyConfig
{
    /// <summary>
    /// Policy type.
    /// Valid values: "throw", "fallbackToControl", "fallbackTo", "tryInOrder", "tryAny".
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// For "fallbackTo" policy, the specific condition key to fall back to.
    /// </summary>
    public string? FallbackKey { get; set; }

    /// <summary>
    /// For "tryInOrder" policy, the ordered list of fallback condition keys.
    /// </summary>
    public List<string>? FallbackKeys { get; set; }
}
