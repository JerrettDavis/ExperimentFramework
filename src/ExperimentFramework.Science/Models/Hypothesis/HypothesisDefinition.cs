namespace ExperimentFramework.Science.Models.Hypothesis;

/// <summary>
/// Represents a complete hypothesis definition for a scientific experiment.
/// </summary>
/// <remarks>
/// <para>
/// A hypothesis definition formalizes what the experiment is testing, including:
/// <list type="bullet">
/// <item><description>The null and alternative hypotheses</description></item>
/// <item><description>Primary and secondary endpoints</description></item>
/// <item><description>Success criteria and thresholds</description></item>
/// <item><description>Expected effect sizes for power analysis</description></item>
/// </list>
/// </para>
/// <para>
/// Defining hypotheses before running an experiment is essential for scientific
/// rigor and prevents p-hacking or post-hoc rationalization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var hypothesis = new HypothesisDefinition
/// {
///     Name = "Checkout V2 Superiority",
///     NullHypothesis = "The new checkout has no effect on conversion",
///     AlternativeHypothesis = "The new checkout increases conversion rate",
///     Type = HypothesisType.Superiority,
///     PrimaryEndpoint = new Endpoint
///     {
///         Name = "purchase_completed",
///         OutcomeType = OutcomeType.Binary,
///         HigherIsBetter = true
///     },
///     ExpectedEffectSize = 0.05, // 5% improvement
///     SuccessCriteria = new SuccessCriteria
///     {
///         Alpha = 0.05,
///         Power = 0.80,
///         MinimumSampleSize = 1000
///     }
/// };
/// </code>
/// </example>
public sealed class HypothesisDefinition
{
    /// <summary>
    /// Gets the name/identifier for this hypothesis.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets a description of this hypothesis.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the formal statement of the null hypothesis.
    /// </summary>
    /// <example>"The new checkout flow has no effect on conversion rate"</example>
    public required string NullHypothesis { get; init; }

    /// <summary>
    /// Gets the formal statement of the alternative hypothesis.
    /// </summary>
    /// <example>"The new checkout flow increases conversion rate by at least 5%"</example>
    public required string AlternativeHypothesis { get; init; }

    /// <summary>
    /// Gets the type of hypothesis test.
    /// </summary>
    public required HypothesisType Type { get; init; }

    /// <summary>
    /// Gets the primary endpoint for this hypothesis.
    /// </summary>
    /// <remarks>
    /// The primary endpoint is the main outcome measure that determines
    /// whether the hypothesis is supported or rejected.
    /// </remarks>
    public required Endpoint PrimaryEndpoint { get; init; }

    /// <summary>
    /// Gets the secondary endpoints for this hypothesis.
    /// </summary>
    /// <remarks>
    /// Secondary endpoints provide additional insights but do not determine
    /// the primary conclusion of the experiment.
    /// </remarks>
    public IReadOnlyList<Endpoint>? SecondaryEndpoints { get; init; }

    /// <summary>
    /// Gets the expected effect size for power calculations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For binary outcomes: expected difference in proportions (e.g., 0.05 for 5% improvement).
    /// For continuous outcomes: expected standardized effect size (Cohen's d).
    /// </para>
    /// <para>
    /// Used to calculate required sample size for desired statistical power.
    /// </para>
    /// </remarks>
    public required double ExpectedEffectSize { get; init; }

    /// <summary>
    /// Gets the success criteria for this hypothesis.
    /// </summary>
    public required SuccessCriteria SuccessCriteria { get; init; }

    /// <summary>
    /// Gets the control condition identifier.
    /// </summary>
    public string? ControlCondition { get; init; }

    /// <summary>
    /// Gets the treatment conditions being tested.
    /// </summary>
    public IReadOnlyList<string>? TreatmentConditions { get; init; }

    /// <summary>
    /// Gets when this hypothesis was defined.
    /// </summary>
    /// <remarks>
    /// Pre-registration timestamp for scientific rigor.
    /// </remarks>
    public DateTimeOffset? DefinedAt { get; init; }

    /// <summary>
    /// Gets the rationale for this hypothesis.
    /// </summary>
    /// <remarks>
    /// Why we believe the treatment might work, based on prior evidence or theory.
    /// </remarks>
    public string? Rationale { get; init; }

    /// <summary>
    /// Gets any additional metadata for this hypothesis.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <inheritdoc />
    public override string ToString() =>
        $"{Name}: {(Type == HypothesisType.TwoSided ? "H1" : Type.ToString())}: {AlternativeHypothesis}";
}
