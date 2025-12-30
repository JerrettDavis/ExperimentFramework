using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Governance.Policy;

/// <summary>
/// Manages and evaluates experiment policies.
/// </summary>
public interface IPolicyEvaluator
{
    /// <summary>
    /// Registers a policy.
    /// </summary>
    /// <param name="policy">The policy to register.</param>
    void RegisterPolicy(IExperimentPolicy policy);

    /// <summary>
    /// Evaluates all registered policies against the context.
    /// </summary>
    /// <param name="context">The policy context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all policy evaluation results.</returns>
    Task<IReadOnlyList<PolicyEvaluationResult>> EvaluateAllAsync(PolicyContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if all critical policies are compliant.
    /// </summary>
    /// <param name="context">The policy context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if all critical policies are compliant, false otherwise.</returns>
    Task<bool> AreAllCriticalPoliciesCompliantAsync(PolicyContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of policy evaluator.
/// </summary>
public class PolicyEvaluator : IPolicyEvaluator
{
    private readonly ILogger<PolicyEvaluator> _logger;
    private readonly List<IExperimentPolicy> _policies = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyEvaluator"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public PolicyEvaluator(ILogger<PolicyEvaluator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public void RegisterPolicy(IExperimentPolicy policy)
    {
        if (policy == null)
            throw new ArgumentNullException(nameof(policy));

        lock (_policies)
        {
            _policies.Add(policy);
        }

        _logger.LogInformation("Registered policy: {PolicyName}", policy.Name);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PolicyEvaluationResult>> EvaluateAllAsync(PolicyContext context, CancellationToken cancellationToken = default)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        List<IExperimentPolicy> policiesToEvaluate;
        lock (_policies)
        {
            policiesToEvaluate = _policies.ToList();
        }

        var results = new List<PolicyEvaluationResult>();
        foreach (var policy in policiesToEvaluate)
        {
            try
            {
                var result = await policy.EvaluateAsync(context, cancellationToken);
                results.Add(result);

                if (!result.IsCompliant)
                {
                    _logger.LogWarning(
                        "Policy '{PolicyName}' violation for experiment '{ExperimentName}': {Reason} (Severity: {Severity})",
                        policy.Name, context.ExperimentName, result.Reason ?? "none", result.Severity);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating policy '{PolicyName}' for experiment '{ExperimentName}'",
                    policy.Name, context.ExperimentName);

                // Add a failure result
                results.Add(new PolicyEvaluationResult
                {
                    IsCompliant = false,
                    PolicyName = policy.Name,
                    Reason = $"Policy evaluation failed: {ex.Message}",
                    Severity = PolicyViolationSeverity.Error
                });
            }
        }

        return results.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<bool> AreAllCriticalPoliciesCompliantAsync(PolicyContext context, CancellationToken cancellationToken = default)
    {
        var results = await EvaluateAllAsync(context, cancellationToken);
        return results
            .Where(r => r.Severity == PolicyViolationSeverity.Critical)
            .All(r => r.IsCompliant);
    }
}
