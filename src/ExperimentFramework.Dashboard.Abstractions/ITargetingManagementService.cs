namespace ExperimentFramework.Dashboard.Abstractions;

/// <summary>
/// Provides management operations for targeting rules from the dashboard.
/// </summary>
/// <remarks>
/// Implementations bridge the dashboard API to the underlying targeting system
/// (e.g., <c>ITargetingConfigurationProvider</c> from ExperimentFramework.Targeting).
/// Register an implementation if targeting rule management is required.
/// </remarks>
public interface ITargetingManagementService
{
    /// <summary>
    /// Gets all targeting rules for an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The targeting rules for the experiment, or null if not found.</returns>
    Task<IReadOnlyList<TargetingRuleDto>?> GetRulesAsync(
        string experimentName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces all targeting rules for an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="rules">The new set of targeting rules.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetRulesAsync(
        string experimentName,
        IReadOnlyList<TargetingRuleDto> rules,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates targeting rules against the provided context attributes.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="context">The context attributes to evaluate against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The evaluation result.</returns>
    Task<TargetingEvaluationResult> EvaluateAsync(
        string experimentName,
        IReadOnlyDictionary<string, object> context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A targeting rule as a data-transfer object.
/// </summary>
public sealed class TargetingRuleDto
{
    /// <summary>
    /// Gets or sets the rule identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or sets the rule type (e.g., "attribute-equals", "percentage", "user-ids").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets or sets the variant key to select when this rule matches.
    /// </summary>
    public required string VariantKey { get; init; }

    /// <summary>
    /// Gets or sets whether this rule is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets or sets additional parameters for the rule.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Parameters { get; init; }
}

/// <summary>
/// Result of evaluating targeting rules against a context.
/// </summary>
public sealed class TargetingEvaluationResult
{
    /// <summary>
    /// Gets or sets whether any rule matched.
    /// </summary>
    public required bool Matched { get; init; }

    /// <summary>
    /// Gets or sets the matched variant key, or null if no rule matched.
    /// </summary>
    public string? MatchedVariant { get; init; }

    /// <summary>
    /// Gets or sets the ID of the rule that matched, or null if no rule matched.
    /// </summary>
    public string? MatchedRuleId { get; init; }
}
