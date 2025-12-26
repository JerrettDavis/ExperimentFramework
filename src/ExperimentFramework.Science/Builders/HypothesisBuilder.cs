using ExperimentFramework.Data.Models;
using ExperimentFramework.Science.Models.Hypothesis;

namespace ExperimentFramework.Science.Builders;

/// <summary>
/// Fluent builder for creating hypothesis definitions.
/// </summary>
/// <example>
/// <code>
/// var hypothesis = new HypothesisBuilder("checkout-test")
///     .Superiority()
///     .NullHypothesis("New checkout has no effect on conversion")
///     .AlternativeHypothesis("New checkout improves conversion rate")
///     .PrimaryEndpoint("conversion", OutcomeType.Binary, ep => ep
///         .Description("Purchase completion rate")
///         .HigherIsBetter())
///     .ExpectedEffectSize(0.05)
///     .WithSuccessCriteria(c => c
///         .Alpha(0.05)
///         .Power(0.80)
///         .MinimumSampleSize(1000))
///     .Build();
/// </code>
/// </example>
public sealed class HypothesisBuilder
{
    private readonly string _name;
    private string? _description;
    private string? _nullHypothesis;
    private string? _alternativeHypothesis;
    private HypothesisType _type = HypothesisType.TwoSided;
    private Endpoint? _primaryEndpoint;
    private readonly List<Endpoint> _secondaryEndpoints = [];
    private double _expectedEffectSize;
    private SuccessCriteria _successCriteria = new();
    private string? _controlCondition;
    private readonly List<string> _treatmentConditions = [];
    private DateTimeOffset? _definedAt;
    private string? _rationale;
    private Dictionary<string, object>? _metadata;

    /// <summary>
    /// Creates a new hypothesis builder.
    /// </summary>
    /// <param name="name">The name/identifier for the hypothesis.</param>
    public HypothesisBuilder(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _name = name;
    }

    /// <summary>
    /// Sets the description.
    /// </summary>
    public HypothesisBuilder Description(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Configures a superiority test.
    /// </summary>
    public HypothesisBuilder Superiority()
    {
        _type = HypothesisType.Superiority;
        return this;
    }

    /// <summary>
    /// Configures a non-inferiority test.
    /// </summary>
    public HypothesisBuilder NonInferiority()
    {
        _type = HypothesisType.NonInferiority;
        return this;
    }

    /// <summary>
    /// Configures an equivalence test.
    /// </summary>
    public HypothesisBuilder Equivalence()
    {
        _type = HypothesisType.Equivalence;
        return this;
    }

    /// <summary>
    /// Configures a two-sided test.
    /// </summary>
    public HypothesisBuilder TwoSided()
    {
        _type = HypothesisType.TwoSided;
        return this;
    }

    /// <summary>
    /// Sets the null hypothesis statement.
    /// </summary>
    public HypothesisBuilder NullHypothesis(string statement)
    {
        _nullHypothesis = statement;
        return this;
    }

    /// <summary>
    /// Sets the alternative hypothesis statement.
    /// </summary>
    public HypothesisBuilder AlternativeHypothesis(string statement)
    {
        _alternativeHypothesis = statement;
        return this;
    }

    /// <summary>
    /// Sets the primary endpoint.
    /// </summary>
    public HypothesisBuilder PrimaryEndpoint(
        string name,
        OutcomeType type,
        Action<EndpointBuilder>? configure = null)
    {
        var builder = new EndpointBuilder(name, type);
        configure?.Invoke(builder);
        _primaryEndpoint = builder.Build();
        return this;
    }

    /// <summary>
    /// Adds a secondary endpoint.
    /// </summary>
    public HypothesisBuilder SecondaryEndpoint(
        string name,
        OutcomeType type,
        Action<EndpointBuilder>? configure = null)
    {
        var builder = new EndpointBuilder(name, type);
        configure?.Invoke(builder);
        _secondaryEndpoints.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Sets the expected effect size.
    /// </summary>
    public HypothesisBuilder ExpectedEffectSize(double effectSize)
    {
        _expectedEffectSize = effectSize;
        return this;
    }

    /// <summary>
    /// Configures success criteria.
    /// </summary>
    public HypothesisBuilder WithSuccessCriteria(Action<SuccessCriteriaBuilder> configure)
    {
        var builder = new SuccessCriteriaBuilder();
        configure(builder);
        _successCriteria = builder.Build();
        return this;
    }

    /// <summary>
    /// Sets the control condition.
    /// </summary>
    public HypothesisBuilder Control(string condition)
    {
        _controlCondition = condition;
        return this;
    }

    /// <summary>
    /// Adds a treatment condition.
    /// </summary>
    public HypothesisBuilder Treatment(string condition)
    {
        _treatmentConditions.Add(condition);
        return this;
    }

    /// <summary>
    /// Sets the definition timestamp.
    /// </summary>
    public HypothesisBuilder DefinedAt(DateTimeOffset timestamp)
    {
        _definedAt = timestamp;
        return this;
    }

    /// <summary>
    /// Sets the definition timestamp to now.
    /// </summary>
    public HypothesisBuilder DefinedNow()
    {
        _definedAt = DateTimeOffset.UtcNow;
        return this;
    }

    /// <summary>
    /// Sets the rationale.
    /// </summary>
    public HypothesisBuilder Rationale(string rationale)
    {
        _rationale = rationale;
        return this;
    }

    /// <summary>
    /// Adds metadata.
    /// </summary>
    public HypothesisBuilder WithMetadata(string key, object value)
    {
        _metadata ??= [];
        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Builds the hypothesis definition.
    /// </summary>
    public HypothesisDefinition Build()
    {
        if (_nullHypothesis is null)
            throw new InvalidOperationException("Null hypothesis must be specified.");
        if (_alternativeHypothesis is null)
            throw new InvalidOperationException("Alternative hypothesis must be specified.");
        if (_primaryEndpoint is null)
            throw new InvalidOperationException("Primary endpoint must be specified.");

        return new HypothesisDefinition
        {
            Name = _name,
            Description = _description,
            NullHypothesis = _nullHypothesis,
            AlternativeHypothesis = _alternativeHypothesis,
            Type = _type,
            PrimaryEndpoint = _primaryEndpoint,
            SecondaryEndpoints = _secondaryEndpoints.Count > 0 ? _secondaryEndpoints : null,
            ExpectedEffectSize = _expectedEffectSize,
            SuccessCriteria = _successCriteria,
            ControlCondition = _controlCondition,
            TreatmentConditions = _treatmentConditions.Count > 0 ? _treatmentConditions : null,
            DefinedAt = _definedAt,
            Rationale = _rationale,
            Metadata = _metadata
        };
    }
}

/// <summary>
/// Builder for endpoints.
/// </summary>
public sealed class EndpointBuilder
{
    private readonly string _name;
    private readonly OutcomeType _type;
    private string? _description;
    private string? _unit;
    private bool _higherIsBetter = true;
    private double? _minimumImportantDifference;
    private double? _expectedBaselineValue;
    private double? _expectedVariance;

    internal EndpointBuilder(string name, OutcomeType type)
    {
        _name = name;
        _type = type;
    }

    /// <summary>
    /// Sets the description.
    /// </summary>
    public EndpointBuilder Description(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the unit of measurement.
    /// </summary>
    public EndpointBuilder Unit(string unit)
    {
        _unit = unit;
        return this;
    }

    /// <summary>
    /// Indicates higher values are better.
    /// </summary>
    public EndpointBuilder HigherIsBetter()
    {
        _higherIsBetter = true;
        return this;
    }

    /// <summary>
    /// Indicates lower values are better.
    /// </summary>
    public EndpointBuilder LowerIsBetter()
    {
        _higherIsBetter = false;
        return this;
    }

    /// <summary>
    /// Sets the minimum clinically important difference.
    /// </summary>
    public EndpointBuilder MinimumImportantDifference(double value)
    {
        _minimumImportantDifference = value;
        return this;
    }

    /// <summary>
    /// Sets the expected baseline value.
    /// </summary>
    public EndpointBuilder ExpectedBaseline(double value)
    {
        _expectedBaselineValue = value;
        return this;
    }

    /// <summary>
    /// Sets the expected variance.
    /// </summary>
    public EndpointBuilder ExpectedVariance(double value)
    {
        _expectedVariance = value;
        return this;
    }

    internal Endpoint Build() => new()
    {
        Name = _name,
        OutcomeType = _type,
        Description = _description,
        Unit = _unit,
        HigherIsBetter = _higherIsBetter,
        MinimumImportantDifference = _minimumImportantDifference,
        ExpectedBaselineValue = _expectedBaselineValue,
        ExpectedVariance = _expectedVariance
    };
}

/// <summary>
/// Builder for success criteria.
/// </summary>
public sealed class SuccessCriteriaBuilder
{
    private double _alpha = 0.05;
    private double _power = 0.80;
    private int? _minimumSampleSize;
    private double? _minimumEffectSize;
    private double? _nonInferiorityMargin;
    private double? _equivalenceMargin;
    private bool _primaryEndpointOnly = true;
    private bool _applyMultipleComparisonCorrection = true;
    private TimeSpan? _minimumDuration;
    private bool _requirePositiveEffect = true;

    /// <summary>
    /// Sets the significance level.
    /// </summary>
    public SuccessCriteriaBuilder Alpha(double alpha)
    {
        _alpha = alpha;
        return this;
    }

    /// <summary>
    /// Sets the desired power.
    /// </summary>
    public SuccessCriteriaBuilder Power(double power)
    {
        _power = power;
        return this;
    }

    /// <summary>
    /// Sets the minimum sample size per group.
    /// </summary>
    public SuccessCriteriaBuilder MinimumSampleSize(int size)
    {
        _minimumSampleSize = size;
        return this;
    }

    /// <summary>
    /// Sets the minimum effect size.
    /// </summary>
    public SuccessCriteriaBuilder MinimumEffectSize(double size)
    {
        _minimumEffectSize = size;
        return this;
    }

    /// <summary>
    /// Sets the non-inferiority margin.
    /// </summary>
    public SuccessCriteriaBuilder NonInferiorityMargin(double margin)
    {
        _nonInferiorityMargin = margin;
        return this;
    }

    /// <summary>
    /// Sets the equivalence margin.
    /// </summary>
    public SuccessCriteriaBuilder EquivalenceMargin(double margin)
    {
        _equivalenceMargin = margin;
        return this;
    }

    /// <summary>
    /// Only requires the primary endpoint to be significant.
    /// </summary>
    public SuccessCriteriaBuilder PrimaryEndpointOnly()
    {
        _primaryEndpointOnly = true;
        return this;
    }

    /// <summary>
    /// Requires all endpoints to be significant.
    /// </summary>
    public SuccessCriteriaBuilder AllEndpoints()
    {
        _primaryEndpointOnly = false;
        return this;
    }

    /// <summary>
    /// Applies multiple comparison correction.
    /// </summary>
    public SuccessCriteriaBuilder WithMultipleComparisonCorrection()
    {
        _applyMultipleComparisonCorrection = true;
        return this;
    }

    /// <summary>
    /// Disables multiple comparison correction.
    /// </summary>
    public SuccessCriteriaBuilder NoMultipleComparisonCorrection()
    {
        _applyMultipleComparisonCorrection = false;
        return this;
    }

    /// <summary>
    /// Sets the minimum duration.
    /// </summary>
    public SuccessCriteriaBuilder MinimumDuration(TimeSpan duration)
    {
        _minimumDuration = duration;
        return this;
    }

    /// <summary>
    /// Requires a positive effect direction, not just significance.
    /// </summary>
    public SuccessCriteriaBuilder RequirePositiveEffect()
    {
        _requirePositiveEffect = true;
        return this;
    }

    /// <summary>
    /// Only requires significance, not a specific direction.
    /// </summary>
    public SuccessCriteriaBuilder AnySignificantEffect()
    {
        _requirePositiveEffect = false;
        return this;
    }

    internal SuccessCriteria Build() => new()
    {
        Alpha = _alpha,
        Power = _power,
        MinimumSampleSize = _minimumSampleSize,
        MinimumEffectSize = _minimumEffectSize,
        NonInferiorityMargin = _nonInferiorityMargin,
        EquivalenceMargin = _equivalenceMargin,
        PrimaryEndpointOnly = _primaryEndpointOnly,
        ApplyMultipleComparisonCorrection = _applyMultipleComparisonCorrection,
        MinimumDuration = _minimumDuration,
        RequirePositiveEffect = _requirePositiveEffect
    };
}
