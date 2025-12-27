namespace ExperimentFramework.Configuration.Models;

/// <summary>
/// Configuration for a scientific experiment hypothesis (requires ExperimentFramework.Science).
/// </summary>
public sealed class HypothesisConfig
{
    /// <summary>
    /// Unique name for this hypothesis.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional description of the hypothesis.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Hypothesis type.
    /// Valid values: "superiority", "nonInferiority", "equivalence", "twoSided".
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Statement of the null hypothesis (H0).
    /// </summary>
    public required string NullHypothesis { get; set; }

    /// <summary>
    /// Statement of the alternative hypothesis (H1).
    /// </summary>
    public required string AlternativeHypothesis { get; set; }

    /// <summary>
    /// Primary endpoint configuration.
    /// </summary>
    public required EndpointConfig PrimaryEndpoint { get; set; }

    /// <summary>
    /// Secondary endpoints (optional).
    /// </summary>
    public List<EndpointConfig>? SecondaryEndpoints { get; set; }

    /// <summary>
    /// Expected effect size (Cohen's d or similar).
    /// </summary>
    public required double ExpectedEffectSize { get; set; }

    /// <summary>
    /// Success criteria configuration.
    /// </summary>
    public required SuccessCriteriaConfig SuccessCriteria { get; set; }

    /// <summary>
    /// Name of the control condition.
    /// </summary>
    public string? ControlCondition { get; set; }

    /// <summary>
    /// Names of the treatment conditions.
    /// </summary>
    public List<string>? TreatmentConditions { get; set; }

    /// <summary>
    /// Rationale for the hypothesis.
    /// </summary>
    public string? Rationale { get; set; }

    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
